using Esprima;
using Jint.Runtime;
using Jint.Runtime.Interop;
using ReactUnity.DomProxies;
using ReactUnity.Helpers;
using ReactUnity.Interop;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace ReactUnity
{
    public class ReactUnityRunner
    {
        public Jint.Engine engine { get; private set; }
        public ReactContext context { get; private set; }

        public void RunScript(string script, ReactContext ctx, UnityEvent<ReactUnityRunner> beforeStart = null, UnityEvent<ReactUnityRunner> afterStart = null)
        {
            if (string.IsNullOrWhiteSpace(script)) return;

            context = ctx;
            if (engine == null) CreateBaseEngine();
            CreateLocation(engine);
            CreateConsole(engine);
            CreateLocalStorage(engine);
            CreateScheduler(engine, context);
            engine.SetValue("RootContainer", context.Host);
            engine.SetValue("Globals", context.Globals);

            var beforeStartCallbacks = new List<Action<ReactUnityRunner>>() { (e) => beforeStart.Invoke(e) };
            var afterStartCallbacks = new List<Action<ReactUnityRunner>>() { (e) => afterStart.Invoke(e) };

            engine.SetValue("addEventListener", new Action<string, Action<ReactUnityRunner>>((e, f) =>
            {
                if (e == "DOMContentLoaded") afterStartCallbacks.Add(f);
            }));

            try
            {
                beforeStartCallbacks.ForEach(x => x?.Invoke(this));
                engine.Execute(script);
                afterStartCallbacks.ForEach(x => x?.Invoke(this));
            }
            catch (ParserException ex)
            {
                Debug.LogError($"Parser exception in line {ex.LineNumber} column {ex.Column}");
                Debug.LogException(ex);
            }
            catch (JavaScriptException ex)
            {
                Debug.LogError($"JS exception in line {ex.LineNumber} column {ex.Column}");
                Debug.LogException(ex);
            }
            catch (JintException ex)
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

        void CreateBaseEngine()
        {
            engine = new Jint.Engine(x =>
            {
                x.AllowClr(
                    typeof(Convert).Assembly,
#if UNITY_EDITOR
                    typeof(UnityEditor.EditorWindow).Assembly,
                    typeof(GUILayout).Assembly,
                    typeof(UnityEngine.UIElements.StyleLength).Assembly,
#endif
                    typeof(Vector3).Assembly,
                    typeof(Component).Assembly,
                    typeof(ReactUnityRunner).Assembly
                );
                x.CatchClrExceptions(ex =>
                {
                    Debug.LogException(ex);
                    return true;
                });
            });

            engine.SetValue("log", new Func<object, object>((x) => { Debug.Log(x); return x; }));
            engine.Execute("__dirname = '';");
            engine.Execute("WeakMap = Map;");
            engine.Execute("globalThis = global = window = parent = this;");
            engine.Execute("setTimeout = setInterval = clearTimeout = clearInterval = null;");
            engine.Execute("btoa = atob = null;");
            engine.Execute("process = { env: { NODE_ENV: 'production' }, argv: [], on: () => {} };");

            engine.SetValue("Engine", engine);
            engine.SetValue("Callback", typeof(Callback));

            engine.SetValue("importType", new ClrFunctionInstance(
                engine,
                "importType",
                func: (thisObj, arguments) => TypeReference.CreateTypeReference(engine,
                    ReflectionHelpers.FindType(TypeConverter.ToString(arguments.At(0)),
                    arguments.Length > 1 ? TypeConverter.ToBoolean(arguments.At(1)) : false))));

            engine.SetValue("UnityEngine", new NamespaceReference(engine, "UnityEngine"));
            engine.SetValue("ReactUnity", new NamespaceReference(engine, "ReactUnity"));
            engine.SetValue("Facebook", new NamespaceReference(engine, "Facebook"));
#if UNITY_EDITOR
            engine.SetValue("UnityEditor", new NamespaceReference(engine, "UnityEditor"));
#endif

            // Load polyfills
            engine.Execute(Resources.Load<TextAsset>("ReactUnity/polyfills/promise").text);
            engine.Execute(Resources.Load<TextAsset>("ReactUnity/polyfills/base64").text);
            engine.Execute(Resources.Load<TextAsset>("ReactUnity/polyfills/fetch").text);

            engine.SetValue("Unity", new ReactUnityAPI());
        }

        void CreateConsole(Jint.Engine engine)
        {
            var console = new ConsoleProxy(context);

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
    }
}
