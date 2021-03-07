using ReactUnity.Editor.Renderer.Components;
using ReactUnity.Editor.Renderer.Styling;
using ReactUnity.Helpers;
using ReactUnity.Interop;
using ReactUnity.Schedulers;
using ReactUnity.Types;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor.UIElements;

namespace ReactUnity.Editor.Renderer
{
    public class EditorContext : ReactContext
    {
        public static Func<string, string, EditorContext, IEditorReactComponent<VisualElement>> defaultCreator =
            (tag, text, context) => new EditorReactComponent<Box>(context, tag);

        public static Func<string, EditorContext, ITextComponent> textCreator =
            (text, context) => new EditorTextComponent(text, context, "_text");

        public static Dictionary<string, Func<string, string, EditorContext, IEditorReactComponent<VisualElement>>> ComponentCreators
            = new Dictionary<string, Func<string, string, EditorContext, IEditorReactComponent<VisualElement>>>()
            {
                { "text", (tag, text, context) => new EditorTextComponent(text, context, tag) },
                { "button", (tag, text, context) => new EditorButtonComponent(context) },
                { "view", (tag, text, context) => new EditorReactComponent<Box>(context, "view") },
                { "toggle", (tag, text, context) => new EditorReactComponent<Toggle>(context, "toggle") },
                { "image", (tag, text, context) => new EditorReactComponent<Image>(context, "image") },
                { "scroll", (tag, text, context) => new EditorReactComponent<ScrollView>(context, "scroll") },
                { "input", (tag, text, context) => new EditorReactComponent<TextField>(context, "input") },
                { "helpbox", (tag, text, context) => new EditorReactComponent<HelpBox>(context, "helpbox") },
                { "foldout", (tag, text, context) => new EditorReactComponent<Foldout>(context, "foldout") },
                { "popup", (tag, text, context) => new EditorReactComponent<PopupWindow>(context, "popup") },
                { "slider", (tag, text, context) => new EditorReactComponent<Slider>(context, "slider") },
                { "sliderint", (tag, text, context) => new EditorReactComponent<SliderInt>(context, "sliderint") },
                { "range", (tag, text, context) => new EditorReactComponent<MinMaxSlider>(context, "range") },
                { "repeat", (tag, text, context) => new EditorReactComponent<RepeatButton>(context, "repeat") },
                { "scroller", (tag, text, context) => new EditorReactComponent<Scroller>(context, "scroller") },
                { "list", (tag, text, context) => new EditorReactComponent<ListView>(context, "list") },
                { "imgui", (tag, text, context) => new EditorReactComponent<IMGUIContainer>(context, "imgui") },
                { "template", (tag, text, context) => new EditorReactComponent<TemplateContainer>(context, "template") },
                { "color", (tag, text, context) => new EditorReactComponent<ColorField>(context, "color") },
                { "bounds", (tag, text, context) => new EditorReactComponent<BoundsField>(context, "bounds") },
                { "boundsint", (tag, text, context) => new EditorReactComponent<BoundsIntField>(context, "boundsint") },
                { "curve", (tag, text, context) => new EditorReactComponent<CurveField>(context, "curve") },
                { "double", (tag, text, context) => new EditorReactComponent<DoubleField>(context, "double") },
                { "enum", (tag, text, context) => new EditorReactComponent<EnumField>(context, "enum") },
                { "flags", (tag, text, context) => new EditorReactComponent<EnumFlagsField>(context, "flags") },
                { "float", (tag, text, context) => new EditorReactComponent<FloatField>(context, "float") },
                { "gradient", (tag, text, context) => new EditorReactComponent<GradientField>(context, "gradient") },
                { "inspector", (tag, text, context) => new EditorReactComponent<InspectorElement>(context, "inspector") },
                { "integer", (tag, text, context) => new EditorReactComponent<IntegerField>(context, "integer") },
                { "layer", (tag, text, context) => new EditorReactComponent<LayerField>(context, "layer") },
                { "layermask", (tag, text, context) => new EditorReactComponent<LayerMaskField>(context, "layermask") },
                { "long", (tag, text, context) => new EditorReactComponent<LongField>(context, "long") },
                { "mask", (tag, text, context) => new EditorReactComponent<MaskField>(context, "mask") },
                { "object", (tag, text, context) => new EditorReactComponent<ObjectField>(context, "object") },
                { "progress", (tag, text, context) => new EditorReactComponent<ProgressBar>(context, "progress") },
                { "property", (tag, text, context) => new EditorReactComponent<PropertyField>(context, "property") },
                { "rect", (tag, text, context) => new EditorReactComponent<RectField>(context, "rect") },
                { "rectint", (tag, text, context) => new EditorReactComponent<RectIntField>(context, "rectint") },
                { "tag", (tag, text, context) => new EditorReactComponent<TagField>(context, "tag") },
                { "vector2", (tag, text, context) => new EditorReactComponent<Vector2Field>(context, "vector2") },
                { "vector2int", (tag, text, context) => new EditorReactComponent<Vector2IntField>(context, "vector2int") },
                { "vector3", (tag, text, context) => new EditorReactComponent<Vector3Field>(context, "vector3") },
                { "vector3int", (tag, text, context) => new EditorReactComponent<Vector3IntField>(context, "vector3int") },
                { "vector4", (tag, text, context) => new EditorReactComponent<Vector4Field>(context, "vector4") },
                { "toolbar", (tag, text, context) => new EditorReactComponent<Toolbar>(context, "toolbar") },
                { "tb-breadcrumbs", (tag, text, context) => new EditorReactComponent<ToolbarBreadcrumbs>(context, "tb-breadcrumbs") },
                { "tb-button", (tag, text, context) => new EditorReactComponent<ToolbarButton>(context, "tb-button") },
                { "tb-menu", (tag, text, context) => new EditorReactComponent<ToolbarMenu>(context, "tb-menu") },
                { "tb-popupsearch", (tag, text, context) => new EditorReactComponent<ToolbarPopupSearchField>(context, "tb-popupsearch") },
                { "tb-search", (tag, text, context) => new EditorReactComponent<ToolbarSearchField>(context, "tb-search") },
                { "tb-spacer", (tag, text, context) => new EditorReactComponent<ToolbarSpacer>(context, "tb-spacer") },
                { "tb-toggle", (tag, text, context) => new EditorReactComponent<ToolbarToggle>(context, "tb-toggle") },
            };

        public EditorContext(VisualElement hostElement, StringObjectDictionary globals, ReactScript script, IUnityScheduler scheduler, bool isDevServer, Action onRestart = null)
            : base(globals, script, scheduler, isDevServer, onRestart, true)
        {
            Host = new EditorHostComponent(hostElement, this);
            InsertStyle(EditorResourcesHelper.UseragentStylesheet?.text, -1);
            Host.ResolveStyle(true);

            EditorDispatcher.AddCallOnLateUpdate(() =>
            {
                if (Scheduled)
                {
                    Scheduled = false;

                    for (int i = 0; i < ScheduledCallbacks.Count; i++)
                        ScheduledCallbacks[i]?.Invoke();
                }
            });
        }

        public override ITextComponent CreateText(string text)
        {
            return textCreator(text, this);
        }

        public override IReactComponent CreateComponent(string tag, string text)
        {
            IEditorReactComponent<VisualElement> res = null;
            if (ComponentCreators.TryGetValue(tag, out var creator))
                res = creator(tag, text, this);
            else res = defaultCreator(tag, text, this);
            if (res.Element != null) res.Element.name = $"<{tag}>";
            return res;
        }
    }
}