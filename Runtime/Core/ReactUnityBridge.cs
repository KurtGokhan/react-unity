using ReactUnity.Helpers.TypescriptUtils;
using ReactUnity.Helpers;
using System;
using System.Diagnostics.CodeAnalysis;

namespace ReactUnity
{
    [TypescriptInclude]
    internal class ReactUnityBridge
    {
        private static ReactUnityBridge instance;
        public static ReactUnityBridge Instance => instance ??= new ReactUnityBridge();

        private ReactUnityBridge() { }

        #region Creation

        public ITextComponent createText(string text, IHostComponent host)
        {
            return host.Context.CreateText(text);
        }

        public IReactComponent createElement(string tag, string text, IHostComponent host)
        {
            return host.Context.CreateComponent(tag, text);
        }

        #endregion


        #region Layout

        public void appendChild(object parent, object child)
        {
            if (parent is IContainerComponent p)
                if (child is IReactComponent c)
                    c.SetParent(p);
        }

        public void appendChildToContainer(object parent, object child)
        {
            if (parent is IHostComponent p)
                if (child is IReactComponent c)
                    c.SetParent(p);
        }

        public void insertBefore(object parent, object child, object beforeChild)
        {
            if (parent is IContainerComponent p)
                if (child is IReactComponent c)
                    if (beforeChild is IReactComponent b)
                        c.SetParent(p, b);
        }

        public void removeChild(object parent, object child)
        {
            if (child is IReactComponent c)
                c.Destroy();
        }

        #endregion


        #region Properties

        public void setText(object instance, string text)
        {
            if (instance is ITextComponent c)
                c.SetText(text);
        }

        public void setProperty(object element, string property, object value)
        {
            if (element is IReactComponent c)
                c.SetProperty(property, value);
        }

        public void setData(object element, string property, object value)
        {
            if (element is IReactComponent c)
                c.SetData(property, value);
        }

        public void setEventListener(object element, string eventType, object value)
        {
            if (element is IReactComponent c)
                c.SetEventListener(eventType, Callback.From(value));
        }

        #endregion

        [ExcludeFromCodeCoverage]
        public string getVersion()
        {
#if UNITY_EDITOR
            try
            {
                var packageInfo = UnityEditor.PackageManager.PackageInfo.FindForAssembly(typeof(ReactUnityBridge).Assembly);
                if (packageInfo != null) return packageInfo.version;
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.LogException(ex);
            }
#endif
            return null;
        }
    }
}
