﻿using System;
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using Sirenix.OdinInspector;
#if UNITY_EDITOR && ODIN_INSPECTOR
using Sirenix.Utilities.Editor;
#endif
using ObjectFieldAlignment = Sirenix.OdinInspector.ObjectFieldAlignment;

namespace Rhinox.Utilities
{
    [HideReferenceObjectPicker]
    public class TextureInput
    {
        [PreviewField(ObjectFieldAlignment.Left, Height = 100), HorizontalGroup("G"), HorizontalGroup("G/Tex", MaxWidth = 100), HideLabel]
        public Texture2D texture;

        [VerticalGroup("G/Channels"), CustomValueDrawer(nameof(Draw)), HideLabel] public TextureChannelInput Red    = new TextureChannelInput(TextureChannel.Red);
        [VerticalGroup("G/Channels"), CustomValueDrawer(nameof(Draw)), HideLabel] public TextureChannelInput Green  = new TextureChannelInput(TextureChannel.Green);
        [VerticalGroup("G/Channels"), CustomValueDrawer(nameof(Draw)), HideLabel] public TextureChannelInput Blue   = new TextureChannelInput(TextureChannel.Blue);
        [VerticalGroup("G/Channels"), CustomValueDrawer(nameof(Draw)), HideLabel] public TextureChannelInput Alpha  = new TextureChannelInput(TextureChannel.Alpha);

        public Dictionary<TextureChannel, TextureChannelInput> Inputs
        {
            get
            {
                return new Dictionary<TextureChannel, TextureChannelInput>
                {
                    {TextureChannel.Red, Red},
                    {TextureChannel.Green, Green},
                    {TextureChannel.Blue, Blue},
                    {TextureChannel.Alpha, Alpha}
                };
            }
        }


        public TextureChannelInput GetChannelInput(TextureChannel channel)
        {
            return Inputs[channel];
        }

        public void SetChannelInput(TextureChannel channel, TextureChannelInput channelInput)
        {
            Inputs[channel] = channelInput;
        }
        
        private static TextureChannelInput Draw(TextureChannelInput value, GUIContent label)
        {
#if UNITY_EDITOR
    #if ODIN_INSPECTOR
            SirenixEditorGUI.BeginToolbarBox();
            EditorGUILayout.BeginHorizontal();

            value.Enabled = EditorGUILayout.ToggleLeft("", value.Enabled, GUILayout.Width(10));
            value.Output = (TextureChannel) SirenixEditorFields.EnumDropdown(string.Format("{0} > ", label.text), value.Output);
            
            EditorGUILayout.EndHorizontal();
            SirenixEditorGUI.EndToolbarBox();
    #else
            // TODO: should we reference GUIUtils.editor?
            //CustomEditorGUI.BeginHorizontalToolbar();
            EditorGUILayout.BeginHorizontal();

            value.Enabled = EditorGUILayout.ToggleLeft("", value.Enabled, GUILayout.Width(10));
            value.Output = (TextureChannel) EditorGUILayout.EnumPopup(string.Format("{0} > ", label.text), value.Output);
            
            EditorGUILayout.EndHorizontal();
            //CustomEditorGUI.EndHorizontalToolbar();
    #endif
#endif

            return value;
        }
    }
}