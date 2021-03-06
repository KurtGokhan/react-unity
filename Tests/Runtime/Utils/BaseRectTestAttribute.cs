using NUnit.Framework;
using NUnit.Framework.Interfaces;
using ReactUnity.Helpers;
using ReactUnity.UGUI;
using System.Collections;
using UnityEngine;
using UnityEngine.TestTools;

namespace ReactUnity.Tests
{
    public abstract class BaseReactTestAttribute : LoadSceneAttribute
    {
        public const string DefaultSceneName = "Packages/com.reactunity.core/Tests/Runtime/TestScene.unity";

#if UNITY_EDITOR
        #region Test Debug Toggle
        const string MenuName = "React/Tests/Debug Tests";
        public static bool IsDebugEnabled
        {
            get { return UnityEditor.EditorPrefs.GetBool(MenuName, false); }
            set { UnityEditor.EditorPrefs.SetBool(MenuName, value); }
        }

        [UnityEditor.MenuItem(MenuName)]
        private static void ToggleTests()
        {
            IsDebugEnabled = !IsDebugEnabled;
        }

        [UnityEditor.MenuItem(MenuName, true)]
        private static bool ToggleTestsValidate()
        {
            UnityEditor.Menu.SetChecked(MenuName, IsDebugEnabled);
            return true;
        }
        #endregion
#else
        public static bool IsDebugEnabled => false;
#endif

        public bool AutoRender;

        public BaseReactTestAttribute(string customScene = null, bool autoRender = true) :
            base(customScene ?? DefaultSceneName)
        {
            AutoRender = autoRender;
        }

        public override IEnumerator BeforeTest(ITest test)
        {
            yield return base.BeforeTest(test);

            var canvas = GameObject.Find("REACT_CANVAS");
            Debug.Assert(canvas != null, "The scene must include a canvas object named as REACT_CANVAS");
            var ru = canvas.GetComponent<ReactUnityUGUI>();

            ru.Script = GetScript();
            ru.Globals["test"] = test;
            var sd = new SerializableDictionary();
            ru.Globals["inner"] = sd;

            ru.BeforeStart.AddListener(BeforeStart);
            ru.AfterStart.AddListener(AfterStart);

            // TODO: find out why is Fixture null
            var testBase = test.Fixture as TestBase;
            if (testBase != null) ru.EngineType = testBase.EngineType;
            else ru.EngineType = test.FullName.Contains("(ClearScript)") ? ScriptEngine.JavascriptEngineType.ClearScript : ScriptEngine.JavascriptEngineType.Jint;

            ru.AutoRender = false;
            ru.enabled = true;

            if (IsDebugEnabled)
            {
                ru.Debug = true;
                ru.AwaitDebugger = true;
            }

            if (AutoRender) ru.Render();
        }

        public override IEnumerator AfterTest(ITest test)
        {
            yield return null;
        }


        public virtual void BeforeStart(ReactUnityRunner runner)
        {
            runner.engine.SetValue("Assert", typeof(Assert));
            runner.engine.SetValue("Has", typeof(Has));
            runner.engine.SetValue("Is", typeof(Is));
            runner.engine.SetValue("Iz", typeof(Iz));
            runner.engine.SetValue("Contains", typeof(Contains));
            runner.engine.SetValue("Does", typeof(Does));
            runner.engine.SetValue("Assume", typeof(Assume));
            runner.engine.SetValue("Throws", typeof(Throws));
            runner.engine.SetValue("LogAssert", typeof(LogAssert));
        }


        public virtual void AfterStart(ReactUnityRunner runner)
        {

        }

        public abstract ScriptSource GetScript();
    }
}
