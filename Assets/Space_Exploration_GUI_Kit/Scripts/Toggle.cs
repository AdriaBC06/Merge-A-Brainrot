using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace SpaceGUI
{
    public class Toggle : MonoBehaviour
    {
        public Image first;
        public Image second;
        public Image third;
        public Image fourth;
        public Image background;
        int index;

        void Update()
        {
            if (index == 1)
            {
                background.gameObject.SetActive(false);
            }
            if (index == 0)
            {
                background.gameObject.SetActive(true);
            }
        }
        public void On()
        {
            SetState(true, true);
        }
        public void Off()
        {
            SetState(false, true);
        }

        public void SetState(bool enabled, bool playSound)
        {
            index = enabled ? 1 : 0;
            second.gameObject.SetActive(enabled);
            first.gameObject.SetActive(!enabled);
            third.gameObject.SetActive(enabled);
            fourth.gameObject.SetActive(!enabled);
            SettingsManager.SetMusicMuted(!enabled);

            if (playSound)
            {
                SoundManager.Instance?.PlayClick();
            }
        }
    }
}
