using Esprima;
using Jint.Runtime;
using Jint.Runtime.Descriptors;
using Jint.Runtime.Interop;
using ReactUnity.Debugger;
using ReactUnity.DomProxies;
using ReactUnity.Helpers;
using ReactUnity.Interop;
using ReactUnity.Styling.Types;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ReactUnity
{
    public class ReactUnityRunner : IDisposable
    {
        private Jint.Engine engine;
        private ReactContext context;
        private Jint.DebugAgent.DebugAgentServer server;


        public void RunScript(string script, ReactContext ctx, List<TextAsset> preload = null, Action callback = null)
        {
            if (string.IsNullOrWhiteSpace(script)) return;

            context = ctx;
            if (engine == null) CreateEngine();
            CreateLocation(engine);

            List<Action> callbacks = new List<Action>() { callback };

            engine.SetValue("addEventListener", new Action<string, Action>((e, f) =>
            {
                if (e == "DOMContentLoaded") callbacks.Add(f);
            }));

            engine.SetValue("Unity", new ReactUnityAPI(engine));
            engine.SetValue("RootContainer", context.Host);
            engine.SetValue("Globals", context.Globals);

            server = new Jint.DebugAgent.DebugAgentServer(Jint.DebugAgent.DebugAgentServer.Options.WaitForDebuggerOnFirstStatement,
                9222, (owner, domains) => new ChromeDebugProtocolServer(owner, domains), engine);

            try
            {
                if (preload != null) preload.ForEach(x => engine.Execute(x.text));
                engine.Execute(script, new ParserOptions("react"), "react");
                callbacks.ForEach(x => x?.Invoke());
            }
            catch (ParserException ex)
            {
                Debug.LogError($"Parser exception in line {ex.LineNumber} column {ex.Column}");
                Debug.LogException(ex);
            }
            catch (Jint.Runtime.JavaScriptException ex)
            {
                Debug.LogError($"JS exception in line {ex.LineNumber} column {ex.Column}");
                Debug.LogException(ex);
            }
            catch (Jint.Runtime.JintException ex)
            {
                Debug.LogException(ex);
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
            }
        }

        public void ExecuteScript(string script)
        {
            engine.Execute(script);
        }

        void CreateEngine()
        {
            var ct = new System.Threading.CancellationToken();
            engine = new Jint.Engine(x =>
                x.AllowClr(
                    typeof(System.Convert).Assembly,
#if UNITY_EDITOR
                    typeof(UnityEditor.EditorWindow).Assembly,
                    typeof(UnityEngine.GUILayout).Assembly,
                    typeof(UnityEngine.UIElements.StyleLength).Assembly,
#endif
                    typeof(UnityEngine.Vector3).Assembly,
                    typeof(UnityEngine.Component).Assembly,
                    typeof(ReactUnityRunner).Assembly
                ).CatchClrExceptions(ex =>
                {
                    Debug.LogException(ex);
                    return true;
                })
                .DebuggerStatementHandling(Jint.Runtime.Debugger.DebuggerStatementHandling.Script)
                .DebugMode()
                .Constraint(new Jint.Constraints.CancellationConstraint(ct))
            );

            engine.SetValue("log", new Func<object, object>((x) => { Debug.Log(x); return x; }));
            engine.Execute("__dirname = '';");
            engine.Execute("WeakMap = Map;");
            engine.Execute("globalThis = global = window = parent = this;");
            engine.Execute("setTimeout = setInterval = clearTimeout = clearInterval = null;");
            engine.Execute("btoa = atob = null;");
            engine.Execute("process = { env: { NODE_ENV: 'production' }, argv: [], on: () => {} };");

            engine.SetValue("Engine", engine);
            engine.SetValue("Callback", typeof(Callback));

            CreateConsole(engine);
            CreateLocalStorage(engine);
            CreateScheduler(engine, context);

            engine.SetValue("importType", new ClrFunctionInstance(
                engine,
                "importType",
                func: (thisObj, arguments) => TypeReference.CreateTypeReference(engine,
                    ReflectionHelpers.FindType(TypeConverter.ToString(arguments.At(0)),
                    arguments.Length > 1 ? TypeConverter.ToBoolean(arguments.At(1)) : false))));

            engine.SetValue("UnityEngine", new Jint.Runtime.Interop.NamespaceReference(engine, "UnityEngine"));
            engine.SetValue("ReactUnity", new Jint.Runtime.Interop.NamespaceReference(engine, "ReactUnity"));
            engine.SetValue("Facebook", new Jint.Runtime.Interop.NamespaceReference(engine, "Facebook"));
#if UNITY_EDITOR
            engine.SetValue("UnityEditor", new Jint.Runtime.Interop.NamespaceReference(engine, "UnityEditor"));
#endif

            // Load polyfills
            engine.Execute(Resources.Load<TextAsset>("ReactUnity/polyfills/promise").text);
            engine.Execute(Resources.Load<TextAsset>("ReactUnity/polyfills/base64").text);
            engine.Execute(Resources.Load<TextAsset>("ReactUnity/polyfills/fetch").text);
        }

        void CreateConsole(Jint.Engine engine)
        {
            var console = new ConsoleProxy(engine, context);

            engine.SetValue("console", console);
        }

        void CreateScheduler(Jint.Engine engine, ReactContext context)
        {
            var scheduler = context.Scheduler;
            engine.SetValue("UnityScheduler", scheduler);
            engine.Execute("global.setTimeout = function setTimeout(fun, delay) { return UnityScheduler.setTimeout(new Callback(fun), delay); }");
            engine.Execute("global.setInterval = function setInterval(fun, delay) { return UnityScheduler.setInterval(new Callback(fun), delay); }");
            engine.Execute("global.setImmediate = function setImmediate(fun) { return UnityScheduler.setImmediate(new Callback(fun)); }");
            engine.Execute("global.requestAnimationFrame = function requestAnimationFrame(fun) { return UnityScheduler.requestAnimationFrame(new Callback(fun)); }");
            engine.SetValue("clearTimeout", new Action<int?>(scheduler.clearTimeout));
            engine.SetValue("clearInterval", new Action<int?>(scheduler.clearInterval));
            engine.SetValue("clearImmediate", new Action<int?>(scheduler.clearImmediate));
            engine.SetValue("cancelAnimationFrame", new Action<int?>(scheduler.cancelAnimationFrame));
        }

        void CreateLocalStorage(Jint.Engine engine)
        {
            var storage = new LocalStorage();
            engine.SetValue("localStorage", storage);
        }

        void CreateLocation(Jint.Engine engine)
        {
            var location = new DomProxies.Location(context);
            engine.SetValue("location", location);

#if UNITY_EDITOR || REACT_DEV_SERVER_API
            engine.SetValue("ctx", context);
            engine.SetValue("oldWebSocket", typeof(WebSocketProxy));
            engine.Execute(@"WebSocket = function() { return new oldWebSocket(ctx, ...arguments); }");
            engine.SetValue("oldXMLHttpRequest", typeof(XMLHttpRequest));
            engine.Execute(@"XMLHttpRequest = function() { return new oldXMLHttpRequest('" + location.origin + @"'); }");
#endif
            engine.SetValue("document", new DocumentProxy(context, this.ExecuteScript, location.origin));
        }

        public void Dispose()
        {
            server?.Dispose();
            server = null;
        }
    }
}
