﻿using UnityEngine;
using SanAndreasUnity.Utilities;

namespace SanAndreasUnity.UI
{

    public class ChatInputDisplay : MonoBehaviour
    {
        string m_chatText = "";



        void Start()
        {
            PauseMenu.onGUI += this.OnPauseMenuGUI;
        }

        void OnPauseMenuGUI()
        {

			string buttonText = "Send";
			Vector2 buttonSize = GUIUtils.CalcScreenSizeForText(buttonText, GUI.skin.button);
			Rect rect = GUIUtils.GetCornerRect(ScreenCorner.BottomRight, buttonSize, new Vector2(40, 40));
			if (GUI.Button(rect, buttonText))
			{
				Chat.ChatManager.SendChatMessageToAllPlayersAsLocalPlayer(m_chatText);
				m_chatText = "";
			}

			float textInputWidth = 200;
			rect.xMin -= textInputWidth;
			rect.xMax -= buttonSize.x + 15;
			m_chatText = GUI.TextField(rect, m_chatText, 100);

        }

    }

}