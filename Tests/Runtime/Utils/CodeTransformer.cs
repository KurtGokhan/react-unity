using ReactUnity.ScriptEngine;
using UnityEngine;

namespace ReactUnity.Tests
{
    public class CodeTransformer
    {
        private static CodeTransformer instance;
        public static CodeTransformer Instance => instance ??= new CodeTransformer();

        public IJavaScriptEngine Engine;
        private IJavaScriptEngineFactory EngineFactory;

        public CodeTransformer(JavascriptEngineType type = JavascriptEngineType.ClearScript)
        {
            EngineFactory = JavascriptEngineHelpers.GetEngineFactory(type);
            Engine = EngineFactory.Create(null, false, false);
            Engine.Execute(Resources.Load<TextAsset>("ReactUnity/tests/scripts/babel-standalone").text);
        }

        public static string TransformCode(string code)
        {
            return Instance.Transform(code);
        }

        public string Transform(string code)
        {
            Engine.SetValue("_codeToTransform", new { code });
            return Engine.Evaluate("Babel.transform(_codeToTransform.code, { presets: ['es2015', 'react'] }).code")?.ToString();
        }
    }
}
