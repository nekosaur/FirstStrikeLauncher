using System;
using System.Windows.Forms;
using FirstStrikeLauncher.Properties;

namespace FirstStrikeLauncher
{
    public partial class PreferencesForm : Form
    {
        public Config Config
        {
            get { return _config; }
        }
        private Config _config;

        public PreferencesForm(Config config)
        {
            InitializeComponent();

            _config = config;

            cbResolution.Items.Add("");
            if (_config.Resolutions.Count <= 0)
                _config.Resolutions.AddRange(Resources.Resolutions.Split(' '));

            _config.Resolutions.Sort(delegate(string s1, string s2)
                {
                    int i1 = int.Parse(s1.Split('x')[0]);
                    int i2 = int.Parse(s2.Split('x')[0]);
                    int result = i1.CompareTo(i2);
                    int i3 = int.Parse(s1.Split('x')[1]);
                    int i4 = int.Parse(s2.Split('x')[1]);
                    return result != 0 ? result : i3.CompareTo(i4);
                });


            cbResolution.Items.AddRange(_config.Resolutions.ToArray());

            if (_config.Arguments.ContainsKey("+fullscreen"))
            {
                if (_config.Arguments["+fullscreen"] == "1")
                    cbWindowed.Checked = false;
                else
                    cbWindowed.Checked = true;
            }

            if (_config.Options.ContainsKey("-localUpdate"))
                cbLocalUpdate.Checked = _config.Options["-localUpdate"];

            string resolution = "";

            if (_config.Arguments.ContainsKey("+szx"))
            {
                resolution = _config.Arguments["+szx"] + "x";

                if (_config.Arguments.ContainsKey("+szy"))
                {
                    resolution += _config.Arguments["+szy"];

                    for (int i = 0; i < cbResolution.Items.Count; i++)
                    {
                        if (resolution == (string)cbResolution.Items[i])
                        {
                            cbResolution.SelectedIndex = i;
                            break;
                        }
                    }
                }
            }
        }

        private void btnSave_Click(object sender, EventArgs e)
        {

            if (string.IsNullOrEmpty(cbResolution.Text))
            {
                _config.Arguments.Remove("+szx");
                _config.Arguments.Remove("+szy");
                _config.Arguments.Remove("+widescreen");

                DialogResult = DialogResult.OK;
                Close();

                return;
            }

            if (!cbResolution.Items.Contains(cbResolution.Text))
            {
                string[] values = cbResolution.Text.Split('x');

                if (values.Length == 2)
                {
                    int width, height;

                    if (int.TryParse(values[0], out width) && int.TryParse(values[1], out height))
                    {
                        MessageBox.Show("New resolution seems to be valid. It will be selected and added to the list", "Information", MessageBoxButtons.OK, MessageBoxIcon.Information);

                        _config.Resolutions.Add(cbResolution.Text);

                        SetResolution(values[0], values[1]);

                        DialogResult = DialogResult.OK;

                        Close();

                        return;
                    }
                    
                }
                
                
                MessageBox.Show("The resolution you've entered does not seem to be valid. Make sure you enter it in the same way the other resolutions are listed.\nOr simply choose one of the available ones.", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
                

            }

            DialogResult = DialogResult.OK;

            Close();
        }

        private void btnCancel_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.Cancel;

            Close();
        }

        private void cbLocalUpdate_CheckedChanged(object sender, EventArgs e)
        {
            if (_config.Options.ContainsKey("-localUpdate"))
            {
                _config.Options["-localUpdate"] = cbLocalUpdate.Checked;
            }
            else
            {
                if (cbLocalUpdate.Checked)
                {
                    _config.Options.Add("-localUpdate", true);
                }
            }
        }

        private void cbWindowed_CheckedChanged(object sender, EventArgs e)
        {
            if (_config.Arguments.ContainsKey("+fullscreen"))
            {
                if (cbWindowed.Checked)
                    _config.Arguments["+fullscreen"] = "0";
                else
                {
                    _config.Arguments["+fullscreen"] = "1";

                    try
                    {
                        _config.Arguments.Remove("+szx");
                        _config.Arguments.Remove("+szy");
                    }
                    catch (Exception)
                    {
                        Console.WriteLine(@"Could not find argument to delete.");
                    }
                }
            }
            else
            {
                if (cbWindowed.Checked)
                    _config.Arguments.Add("+fullscreen", "0");
            }
        }

        private void cbResolution_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(cbResolution.Items[cbResolution.SelectedIndex].ToString()))
                return;

            string[] values = ((string) cbResolution.Items[cbResolution.SelectedIndex]).Split('x');
            string width = values[0];
            string height = values[1];
            
            if (values.Length == 2)
            {
                SetResolution(width, height);
            }
        }

        private void SetResolution(string width, string height)
        {
            try
            {
                _config.Arguments["+szx"] = width;
            }
            catch (Exception)
            {
                throw;
            }

            try
            {
                _config.Arguments["+szy"] = height;
            }
            catch (Exception)
            {
                throw;
            }

            SetWidescreen(width, height);
        }

        private void SetWidescreen(string width, string height)
        {
            float aspect = float.Parse(width) / float.Parse(height);
            if (aspect < 1.3f || aspect > 1.4f)
            {
                _config.Arguments["+widescreen"] = "1";
            }
        }
    }
}
