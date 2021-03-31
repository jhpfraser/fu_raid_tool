using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace RaidUtil
{
    public partial class Socials : Form
    {
        private List<List<SocialButton>> pages;
        private int curPage = 2;
        private const int minPage = 1;
        private const int maxPage = 10;
        private int selBtn = 0;
        private int selPage = 0;

        private string m_toonName;
        private string m_serverName;
        private string m_eqFolder;

        public string AppPath;

        public Socials()
        {
            InitializeComponent();
        }

        private void Socials_Load(object sender, EventArgs e)
        {

        }

        public void LoadSocialsForToon(string toonName, string server, string eqfolder)
        {
            m_toonName = toonName;
            m_serverName = server;
            m_eqFolder = eqfolder;
            try
            {
                pages = FuEQ.GetSocials(m_toonName, m_serverName, m_eqFolder);
            }
            catch (Exception e)
            {
                MessageBox.Show("Unable to find your eq ini files, please check your eq folder is correct");
                return;
            }
            LoadPage(curPage);
        }

        private void LoadPage(int pageNo)
        {
            List<SocialButton> p = pages[pageNo - 1];
            btnSocial1.Text = p[0].Title;
            btnSocial2.Text = p[1].Title;
            btnSocial3.Text = p[2].Title;
            btnSocial4.Text = p[3].Title;
            btnSocial5.Text = p[4].Title;
            btnSocial6.Text = p[5].Title;
            btnSocial7.Text = p[6].Title;
            btnSocial8.Text = p[7].Title;
            btnSocial9.Text = p[8].Title;
            btnSocial10.Text = p[9].Title;
            btnSocial11.Text = p[10].Title;
            btnSocial12.Text = p[11].Title;

            txtPage.Text = "Page " + pageNo.ToString();
        }

        private void LoadPage()
        {
            LoadPage(curPage);
        }

        private void btnNext_Click(object sender, EventArgs e)
        {
            if (curPage < maxPage)
            {
                curPage++;
                LoadPage();
            } else
            {

            }
        }

        private void btnPrev_Click(object sender, EventArgs e)
        {
            if (curPage > minPage)
            {
                curPage--;
                LoadPage();
            }
        }

        private void btnSocial1_Click(object sender, EventArgs e)
        {
            LoadSocialButton(1);
        }

        private void btnSocial2_Click(object sender, EventArgs e)
        {
            LoadSocialButton(2);
        }

        private void btnSocial3_Click(object sender, EventArgs e)
        {
            LoadSocialButton(3);
        }

        private void btnSocial4_Click(object sender, EventArgs e)
        {
            LoadSocialButton(4);
        }

        private void btnSocial5_Click(object sender, EventArgs e)
        {
            LoadSocialButton(5);
        }

        private void btnSocial6_Click(object sender, EventArgs e)
        {
            LoadSocialButton(6);
        }

        private void btnSocial7_Click(object sender, EventArgs e)
        {
            LoadSocialButton(7);
        }

        private void btnSocial8_Click(object sender, EventArgs e)
        {
            LoadSocialButton(8);
        }

        private void btnSocial9_Click(object sender, EventArgs e)
        {
            LoadSocialButton(9);
        }

        private void btnSocial10_Click(object sender, EventArgs e)
        {
            LoadSocialButton(10);
        }

        private void btnSocial11_Click(object sender, EventArgs e)
        {
            LoadSocialButton(11);
        }

        private void btnSocial12_Click(object sender, EventArgs e)
        {
            LoadSocialButton(12);
        }

        private void LoadSocialButton(int which)
        {
            selBtn = which;
            selPage = curPage;
            SocialButton s = pages[selPage - 1][selBtn - 1];
            txtSelectedButton.Text = String.Format("[Pg{0} Bt{1}] - {2}", selPage, selBtn, (s.Title.Length > 0 ? s.Title : "<No Button>"));
            txtCommands.Text = String.Join(Environment.NewLine, s.Lines);
            EnableEdit();
        }

        private void EnableEdit()
        {

        }
        private void DisableEdit()
        {

        }

        private void textBox1_TextChanged(object sender, EventArgs e)
        {
            
        }

        private void button1_Click(object sender, EventArgs e)
        {
            if (selPage != 0 && selBtn != 0)
                AssignButton(new LootButton(AppPath, m_toonName, m_serverName), selPage, selBtn);
        }

        private void button2_Click(object sender, EventArgs e)
        {
            if (selPage != 0 && selBtn != 0)
                AssignButton(new RaidAttendanceButton(AppPath, m_toonName, m_serverName), selPage, selBtn);
        }

        private void button3_Click(object sender, EventArgs e)
        {
            if (selPage != 0 && selBtn != 0)
                AssignButton(new LootLookupButton(AppPath, m_toonName, m_serverName), selPage, selBtn);
        }

        private void button5_Click(object sender, EventArgs e)
        {
            if (selPage != 0 && selBtn != 0)
                AssignButton(new GuildRosterButton(AppPath, m_toonName, m_serverName), selPage, selBtn);
        }

        private void AssignButton(SocialButton which, int pageNo, int btnNo)
        {
            which.Page = pageNo;
            which.Button = btnNo;
            if (
                MessageBox.Show(
                    "You are about to modify your live EQ ini files!\r\nIf your toon is logged in to EQ this won't work!", 
                    "Warning", 
                    MessageBoxButtons.OKCancel
                ) == DialogResult.OK)
            {
                FuEQ.SaveSocial(m_eqFolder, m_toonName, m_serverName, which);
                pages[pageNo - 1][btnNo - 1] = which;
                LoadSocialButton(btnNo);
                LoadPage();
            }
       }

        private void button4_Click(object sender, EventArgs e)
        {
            if (selPage != 0 && selBtn != 0)
                AssignButton(new SocialButton(), selPage, selBtn);
        }

        private void label1_Click(object sender, EventArgs e)
        {

        }


    }
}
