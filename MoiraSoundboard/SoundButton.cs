using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace MoiraSoundboard {
    public class SoundButton : Button {
        public string SoundClip;

        public SoundButton(string title, string clip) {
            this.Content = title;
            this.SoundClip = clip;
        }

    }
}
