using System;
using System.Windows.Forms;

namespace BetterJoyForCemu {
    public partial class Reassign : Form {
        ContextMenuStrip menu_joy_buttons = new ContextMenuStrip();

        public Reassign() {
            InitializeComponent();

            foreach (int i in Enum.GetValues(typeof(Joycon.Button))) {
                ToolStripMenuItem temp = new ToolStripMenuItem(Enum.GetName(typeof(Joycon.Button), i));
                temp.Tag = i;
                menu_joy_buttons.Items.Add(temp);
            }

            menu_joy_buttons.ItemClicked += Menu_joy_buttons_ItemClicked;

            foreach (SplitButton c in new SplitButton[] { btn_capture, btn_home, btn_sl_l, btn_sl_r, btn_sr_l, btn_sr_r, btn_shake, btn_reset_mouse, btn_active_gyro }) {
                c.Tag = c.Name.Substring(4);
                GetPrettyName(c);

                tip_reassign.SetToolTip(c, "Middle-click to clear to default.\r\nRight-click to choose a controller button.");
                c.MouseDown += Remap;
                c.Menu = menu_joy_buttons;
                c.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            }
        }

        private void Menu_joy_buttons_ItemClicked(object sender, ToolStripItemClickedEventArgs e) {
            Control c = sender as Control;

            ToolStripItem clickedItem = e.ClickedItem;

            SplitButton caller = (SplitButton)c.Tag;
            Config.SetValue((string)caller.Tag, "joy_" + (clickedItem.Tag));
            GetPrettyName(caller);
        }

        private void Remap(object sender, MouseEventArgs e) {
            SplitButton c = sender as SplitButton;
            switch (e.Button) {
                case MouseButtons.Middle:
                    Config.SetValue((string)c.Tag, Config.GetDefaultValue((string)c.Tag));
                    GetPrettyName(c);
                    break;
                case MouseButtons.Right:
                    break;
            }
        }

        private void GetPrettyName(Control c) {
            string val;
            switch (val = Config.Value((string)c.Tag)) {
                case "0":
                    if (c == btn_home)
                        c.Text = "Guide";
                    else
                        c.Text = "";
                    break;
                default:
                    c.Text = val.StartsWith("joy_")
                        ? Enum.GetName(typeof(Joycon.Button), Int32.Parse(val.Substring(4)))
                        : "";
                    break;
            }
        }

        private void btn_apply_Click(object sender, EventArgs e) {
            Config.Save();
        }

        private void btn_close_Click(object sender, EventArgs e) {
            btn_apply_Click(sender, e);
            Close();
        }
    }
}
