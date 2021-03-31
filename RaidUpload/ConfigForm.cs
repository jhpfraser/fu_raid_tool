using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO;
using System.Diagnostics;

namespace RaidUtil
{
    public partial class ConfigForm : Form
    {
        public string AppPath;
        private string m_currentToon = "";
        private string m_currentServer = "";

        public ConfigForm()
        {
            InitializeComponent();
            eqFolderFinder.RootFolder = Environment.SpecialFolder.MyComputer;
            UpgradeOld();
            LoadConfig();
        }

        private void UpgradeOld()
        {
            string toon = Cfg.get("EqToon");
            if (toon.Length > 0)
            {
                string server = Cfg.get("EqServerShortName");
                string folder = Cfg.get("EqFolder");
                FuEQ.SetToons(new List<string> { String.Format("{0} - {1}", server, toon) });
                FuEQ.SetEQFolderForToon(toon, server, folder);

                Cfg.delete("EqToon");
                Cfg.delete("EqServerShortName");
                Cfg.delete("EqFolder");
                Cfg.delete("EqLogFolder");

            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            if (!String.IsNullOrEmpty(txtEqFolder.Text))
            {
                eqFolderFinder.SelectedPath = txtEqFolder.Text;
            }
            if (eqFolderFinder.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                txtEqFolder.Text = eqFolderFinder.SelectedPath;
            }
            if (!IsEQFolderValid())
            {
                MessageBox.Show("EQ folder seems invalid.");
            }
        }

        private void btnSave_Click(object sender, EventArgs e)
        {
            if (IsFormValid()) {
                SaveConfig();
                MessageBox.Show("Save Successful.");
            }
        }

        private bool IsFormValid()
        {
            if (!IsEQValid())
            {
                return false;
            }

            // loot minutes
            if (String.IsNullOrEmpty(txtLootMinutes.Text))
            {
                MessageBox.Show("Invalid Loot Minutes");
                return false;
            }

            // loot lookup minutes
            if (String.IsNullOrEmpty(txtLootLookupMinutes.Text))
            {
                MessageBox.Show("Invalid Loot Lookup Minutes");
                return false;
            }

            // website username/pass
            FuHttp h = new FuHttp();
            if (!h.Login(txtFuUsername.Text, txtFuPassword.Text))
            {
                return false;
            }
            
            return true;
        }

        private void SaveConfig()
        {
            FuEQ.SetEQFolderForToon(m_currentToon, m_currentServer, txtEqFolder.Text);
            Cfg.set("LootMinutes", txtLootMinutes.Text);
            Cfg.set("LootLookupMinutes", txtLootLookupMinutes.Text);
            Cfg.setEncrypted("HttpUsername", txtFuUsername.Text);
            Cfg.setEncrypted("HttpPassword", txtFuPassword.Text);
        }

        private void DisableRestOfUI()
        {
            groupBox2.Enabled = false;
            groupBox3.Enabled = false;
            gbTesting.Enabled = false;
            txtEqFolder.Enabled = false;
            btnBrowseEqFolder.Enabled = false;
            btnSocials.Enabled = false;
            button9.Enabled = false;
            btnSave.Enabled = false;
        }

        private void EnableRestOfUI()
        {
            groupBox2.Enabled = true;
            groupBox3.Enabled = true;
            gbTesting.Enabled = true;
            txtEqFolder.Enabled = true;
            btnBrowseEqFolder.Enabled = true;
            btnSocials.Enabled = true;
            button9.Enabled = true;
            btnSave.Enabled = true;
        }


        private void LoadConfig() {

            cmbToons.Items.Clear();
            cmbToons.Items.AddRange(FuEQ.GetToons().ToArray());

            if (cmbToons.Items.Count > 0)
            {
                cmbToons.Enabled = true;
                if (m_currentToon.Length > 0 && m_currentServer.Length > 0)
                {
                    int i = cmbToons.Items.IndexOf(String.Format("{0} - {1}", m_currentServer, m_currentToon));
                    if (i > -1)
                    {
                        cmbToons.SelectedIndex = i;
                    }
                }
                else if (cmbToons.Items.Count > 0)
                {
                    cmbToons.SelectedIndex = 0;
                }
                string[] toonBits = cmbToons.Items[cmbToons.SelectedIndex].ToString().Split(new string[] { " - " }, StringSplitOptions.None);
                if (toonBits.Length == 2)
                {
                    m_currentToon = toonBits[1];
                    m_currentServer = toonBits[0];
                    EnableRestOfUI();
                }
            }
            else
            {
                cmbToons.Enabled = false;
                // force them to add a toon
                DisableRestOfUI();
            }

            txtEqFolder.Text = FuEQ.GetEQFolderForToon(m_currentToon, m_currentServer);
//            txtEqFolder.Text = Cfg.get("EqFolder");
            txtLootMinutes.Text = Cfg.get("LootMinutes");
            txtLootLookupMinutes.Text = Cfg.get("LootLookupMinutes");
            txtEqCharacter.Text = "";
            txtEqServer.Text = "bristle";
            txtFuUsername.Text = Cfg.getEncrypted("HttpUsername");
            txtFuPassword.Text = Cfg.getEncrypted("HttpPassword");
        }

        private void btnCancel_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void button1_Click_1(object sender, EventArgs e)
        {
            FuHttp h = new FuHttp();
            if (h.Login(txtFuUsername.Text, txtFuPassword.Text))
            {
                MessageBox.Show("Login Succeeded");
            }
            //else
           // {
            //    MessageBox.Show("Login Failed!");
            ///}
        }

        private void btnSocials_Click(object sender, EventArgs e)
        {
            if (IsEQValid())
            {
                if (!HasSystemCommandEnabled())
                {
                    if (MessageBox.Show("In order for this program to function, enablesystemcommand=1 must be set in eqclient.ini.\r\nDon't do this if you're in EQ.\r\nProceed with change to ini file?", "Warning", MessageBoxButtons.YesNo) == System.Windows.Forms.DialogResult.Yes)
                    {
                        SetSystemCommandEnabled();
                    }
                    else
                    {
                        // user didnt want to set ini
                        return;
                    }

                }
                Socials socForm = new Socials();
                socForm.AppPath = this.AppPath;
                socForm.LoadSocialsForToon(m_currentToon, m_currentServer, txtEqFolder.Text);
                socForm.ShowDialog();
            }
        }

        private bool HasSystemCommandEnabled()
        {
            Ini ini = new Ini(Path.Combine(txtEqFolder.Text, "eqclient.ini"));
            if (ini.GetSections().Contains("Defaults"))
            {
                if (ini.GetValue("EnableSystemCommand", "Defaults", "0") == "1")
                {
                    return true;
                }
            }
            return false;
        }

        private void SetSystemCommandEnabled()
        {
            Ini ini = new Ini(Path.Combine(txtEqFolder.Text, "eqclient.ini"));
            ini.WriteValue("EnableSystemCommand", "Defaults", "1");
            ini.Save();
        }

        private bool IsEQFolderValid()
        {
            return File.Exists(Path.Combine(txtEqFolder.Text, "eqclient.ini"));
        }

        private bool IsEQValid()
        {
            // check eq folder
            if (String.IsNullOrEmpty(txtEqFolder.Text))
            {
                MessageBox.Show("Pick your EQ folder first");
                return false;
            }
            if (!IsEQFolderValid())
            {
                MessageBox.Show("Unable to locate your EQ files, please check EQ folder");
                return false;
            }

            // check toon/server for empty


            // check that a suitable toon/server combo exists
            if (!File.Exists(Path.Combine(txtEqFolder.Text, String.Format("{0}_{1}.ini", m_currentToon, m_currentServer))))
            {
                MessageBox.Show("There is no ini file for that toon/server.  Please check that the toon/server names are correct");
                return false;
            }
            return true;
        }

        private void button1_Click_3(object sender, EventArgs e)
        {
            Program.SendLogsToServer();
        }

        private void button2_Click_1(object sender, EventArgs e)
        {
            if (Program.UpdateGuildRoster(m_currentToon, m_currentServer))
            {
                Program.SendLogsToServer();
                Process.Start("http://fuworldorder.net/admin/raid/attendance.php");
            }
        }

        private void button3_Click_1(object sender, EventArgs e)
        {
            if (Program.UpdateRaidAttendance(m_currentToon, m_currentServer))
            {
                Program.SendLogsToServer();
                Process.Start("http://fuworldorder.net/admin/raid/attendance.php");
            }
        }

        private void button4_Click_1(object sender, EventArgs e)
        {
            float minutes;
            if (!float.TryParse(txtLootMinutes.Text, out minutes))
            {
                MessageBox.Show("Bad loot minutes");
                return;
            }
            if (Program.UpdateLoot(m_currentToon, m_currentServer, minutes))
            {
                Program.SendLogsToServer();
                Process.Start("http://fuworldorder.net/admin/loot/loot_assignments.php");
            }
        }

        private void button5_Click_1(object sender, EventArgs e)
        {
            float minutes;
            if (!float.TryParse(txtLootLookupMinutes.Text, out minutes))
            {
                MessageBox.Show("Bad loot minutes");
                return;
            }
            Program.DoLootLookup(m_currentToon, m_currentServer, minutes, txtEqCharacter.Text);
        }

        private void button6_Click_1(object sender, EventArgs e)
        {
            Process.Start("explorer.exe", Path.Combine(Path.GetDirectoryName(AppPath), Program.LogFolder));
        }

        private void btnBrowseEqFolder_Click(object sender, EventArgs e)
        {
            if (!String.IsNullOrEmpty(txtEqFolder.Text))
            {
                eqFolderFinder.SelectedPath = txtEqFolder.Text;
            }
            if (eqFolderFinder.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                txtEqFolder.Text = eqFolderFinder.SelectedPath;
            }
            if (!IsEQFolderValid())
            {
                MessageBox.Show("EQ folder seems invalid.");
            }
        }

        private void button7_Click(object sender, EventArgs e)
        {
            Process.Start("http://fuworldorder.net/admin/raid/attendance.php");
        }

        private void button8_Click(object sender, EventArgs e)
        {
            Process.Start("http://fuworldorder.net/admin/loot/loot_assignments.php");
        }

        private void button9_Click(object sender, EventArgs e)
        {
            Button showControls = (Button)sender;

            if (showControls.Text == "Show Test Buttons")
            {
                showControls.Text = "Hide Test Buttons";
                this.Size = new Size(this.Size.Width, this.Size.Height + gbTesting.Height + gbTesting.Margin.Bottom + gbTesting.Margin.Top);
            }
            else
            {
                showControls.Text = "Show Test Buttons";
                this.Size = new Size(this.Size.Width, this.Size.Height - gbTesting.Height - gbTesting.Margin.Bottom - gbTesting.Margin.Top);
            }
        }

        private void label1_Click(object sender, EventArgs e)
        {

        }

        private void button10_Click(object sender, EventArgs e)
        {
            string toonList = String.Join(",", FuEQ.GetToons());
            MessageBox.Show(toonList);
            //MessageBox.Show(FuEQ.GetEQFolderForToon("havanap"));
            //FuEQ.SetEQFolderForToon(txtEqCharacter.Text, txtEqFolder.Text);
        }

        private void ConfigForm_Load(object sender, EventArgs e)
        {

        }

        private void button11_Click(object sender, EventArgs e)
        {
            List<string> toons = FuEQ.GetToons();
            m_currentToon = txtEqCharacter.Text;
            m_currentServer = txtEqServer.Text;
            toons.Add(String.Format("{0} - {1}", m_currentServer, m_currentToon));
            FuEQ.SetToons(toons);
            LoadConfig();
        }

        private void label8_Click(object sender, EventArgs e)
        {

        }

        private void cmbToons_SelectedIndexChanged(object sender, EventArgs e)
        {
            ComboBox cmb = (ComboBox)sender;
            string[] toonBits = cmb.Items[cmb.SelectedIndex].ToString().Split(new string[] { " - " }, StringSplitOptions.None);
            if (toonBits.Length == 2)
            {
                if (m_currentServer != toonBits[0] || m_currentToon != toonBits[1])
                {
                    m_currentServer = toonBits[0];
                    m_currentToon = toonBits[1];
                    LoadConfig();
                }
            }
        }
    }
}
