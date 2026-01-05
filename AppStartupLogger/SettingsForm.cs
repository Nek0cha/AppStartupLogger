using System;
using System.Drawing;
using System.Windows.Forms;

namespace AppStartupLogger
{
    public partial class SettingsForm : Form
    {
        private ListBox appsListBox;
        private TextBox newAppTextBox;
        private Button addButton;
        private Button removeButton;
        private Button saveButton;
        private Button cancelButton;
        private Label titleLabel;
        private Label instructionLabel;

        private AppConfig config;

        public SettingsForm(AppConfig config)
        {
            this.config = config;
            InitializeComponent();
            LoadApps();
        }

        private void InitializeComponent()
        {
            this.Text = "アプリ監視設定";
            this.Size = new Size(500, 450);
            this.BackColor = Color.FromArgb(30, 30, 30);
            this.ForeColor = Color.White;
            this.Font = new Font("Yu Gothic UI", 9F);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;

            titleLabel = new Label
            {
                Text = "監視するアプリケーション",
                Font = new Font("Yu Gothic UI", 14F, FontStyle.Bold),
                ForeColor = Color.White,
                Location = new Point(20, 20),
                AutoSize = true
            };

            instructionLabel = new Label
            {
                Text = "プロセス名（.exe を除く）を入力してください",
                Font = new Font("Yu Gothic UI", 9F),
                ForeColor = Color.FromArgb(200, 200, 200),
                Location = new Point(20, 55),
                AutoSize = true
            };

            appsListBox = new ListBox
            {
                Location = new Point(20, 90),
                Size = new Size(440, 180),
                BackColor = Color.FromArgb(45, 45, 48),
                ForeColor = Color.White,
                Font = new Font("Yu Gothic UI", 10F),
                BorderStyle = BorderStyle.FixedSingle
            };

            newAppTextBox = new TextBox
            {
                Location = new Point(20, 285),
                Size = new Size(300, 25),
                BackColor = Color.FromArgb(45, 45, 48),
                ForeColor = Color.White,
                Font = new Font("Yu Gothic UI", 10F),
                BorderStyle = BorderStyle.FixedSingle
            };

            addButton = new Button
            {
                Text = "追加",
                Location = new Point(330, 283),
                Size = new Size(130, 30),
                BackColor = Color.FromArgb(0, 122, 204),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Yu Gothic UI", 9F, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            addButton.FlatAppearance.BorderSize = 0;
            addButton.Click += AddButton_Click;

            removeButton = new Button
            {
                Text = "選択項目を削除",
                Location = new Point(20, 325),
                Size = new Size(440, 30),
                BackColor = Color.FromArgb(192, 57, 43),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Yu Gothic UI", 9F, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            removeButton.FlatAppearance.BorderSize = 0;
            removeButton.Click += RemoveButton_Click;

            saveButton = new Button
            {
                Text = "保存",
                Location = new Point(250, 370),
                Size = new Size(100, 35),
                BackColor = Color.FromArgb(46, 204, 113),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Yu Gothic UI", 10F, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            saveButton.FlatAppearance.BorderSize = 0;
            saveButton.Click += SaveButton_Click;

            cancelButton = new Button
            {
                Text = "キャンセル",
                Location = new Point(360, 370),
                Size = new Size(100, 35),
                BackColor = Color.FromArgb(127, 140, 141),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Yu Gothic UI", 10F, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            cancelButton.FlatAppearance.BorderSize = 0;
            cancelButton.Click += (s, e) => this.DialogResult = DialogResult.Cancel;

            this.Controls.Add(titleLabel);
            this.Controls.Add(instructionLabel);
            this.Controls.Add(appsListBox);
            this.Controls.Add(newAppTextBox);
            this.Controls.Add(addButton);
            this.Controls.Add(removeButton);
            this.Controls.Add(saveButton);
            this.Controls.Add(cancelButton);
        }

        private void LoadApps()
        {
            appsListBox.Items.Clear();
            foreach (var app in config.Apps)
            {
                appsListBox.Items.Add(app);
            }
        }

        private void AddButton_Click(object? sender, EventArgs e)
        {
            string appName = newAppTextBox.Text.Trim();
            if (string.IsNullOrEmpty(appName))
            {
                MessageBox.Show("アプリ名を入力してください。", "入力エラー",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (config.Apps.Contains(appName))
            {
                MessageBox.Show("このアプリは既に登録されています。", "重複エラー",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            config.Apps.Add(appName);
            appsListBox.Items.Add(appName);
            newAppTextBox.Clear();
        }

        private void RemoveButton_Click(object? sender, EventArgs e)
        {
            if (appsListBox.SelectedItem == null)
            {
                MessageBox.Show("削除する項目を選択してください。", "選択エラー",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            string selectedApp = appsListBox.SelectedItem.ToString()!;
            config.Apps.Remove(selectedApp);
            appsListBox.Items.Remove(selectedApp);
        }

        private void SaveButton_Click(object? sender, EventArgs e)
        {
            if (config.Apps.Count == 0)
            {
                var result = MessageBox.Show(
                    "監視するアプリが1つもありません。\nこのまま保存しますか？",
                    "確認",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question);

                if (result == DialogResult.No)
                    return;
            }

            config.Save();
            this.DialogResult = DialogResult.OK;
        }
    }
}
