namespace XenAdmin.Dialogs
{
    partial class AddStorageLinkSystemDialog
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(AddStorageLinkSystemDialog));
            this.btnCancel = new System.Windows.Forms.Button();
            this.btnOK = new System.Windows.Forms.Button();
            this.tableLayoutPanel1 = new System.Windows.Forms.TableLayoutPanel();
            this.label1 = new System.Windows.Forms.Label();
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.tableLayoutPanelCreds = new System.Windows.Forms.TableLayoutPanel();
            this.UsernameLabel = new System.Windows.Forms.Label();
            this.PasswordLabel = new System.Windows.Forms.Label();
            this.PasswordTextBox = new System.Windows.Forms.TextBox();
            this.UsernameTextBox = new System.Windows.Forms.TextBox();
            this.label2 = new System.Windows.Forms.Label();
            this.NamespaceLabel = new System.Windows.Forms.Label();
            this.IPAddressLabel = new System.Windows.Forms.Label();
            this.StorageAdapterComboBox = new System.Windows.Forms.ComboBox();
            this.NamespaceTextBox = new System.Windows.Forms.TextBox();
            this.IPAddressTextBox = new System.Windows.Forms.TextBox();
            this.PortNumberTextBox = new System.Windows.Forms.TextBox();
            this.PortNumberLabel = new System.Windows.Forms.Label();
            this.flowLayoutPanel2 = new System.Windows.Forms.FlowLayoutPanel();
            this.tableLayoutPanel2 = new System.Windows.Forms.TableLayoutPanel();
            this.tableLayoutPanel1.SuspendLayout();
            this.groupBox1.SuspendLayout();
            this.tableLayoutPanelCreds.SuspendLayout();
            this.flowLayoutPanel2.SuspendLayout();
            this.tableLayoutPanel2.SuspendLayout();
            this.SuspendLayout();
            // 
            // btnCancel
            // 
            resources.ApplyResources(this.btnCancel, "btnCancel");
            this.btnCancel.CausesValidation = false;
            this.btnCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.btnCancel.Name = "btnCancel";
            this.btnCancel.UseVisualStyleBackColor = true;
            this.btnCancel.Click += new System.EventHandler(this.btnCancel_Click);
            // 
            // btnOK
            // 
            resources.ApplyResources(this.btnOK, "btnOK");
            this.btnOK.Name = "btnOK";
            this.btnOK.UseVisualStyleBackColor = true;
            this.btnOK.Click += new System.EventHandler(this.btnOK_Click);
            // 
            // tableLayoutPanel1
            // 
            resources.ApplyResources(this.tableLayoutPanel1, "tableLayoutPanel1");
            this.tableLayoutPanel1.Controls.Add(this.label1, 0, 0);
            this.tableLayoutPanel1.Controls.Add(this.groupBox1, 0, 4);
            this.tableLayoutPanel1.Controls.Add(this.label2, 0, 1);
            this.tableLayoutPanel1.Controls.Add(this.NamespaceLabel, 0, 2);
            this.tableLayoutPanel1.Controls.Add(this.IPAddressLabel, 0, 3);
            this.tableLayoutPanel1.Controls.Add(this.StorageAdapterComboBox, 1, 1);
            this.tableLayoutPanel1.Controls.Add(this.NamespaceTextBox, 1, 2);
            this.tableLayoutPanel1.Controls.Add(this.IPAddressTextBox, 1, 3);
            this.tableLayoutPanel1.Controls.Add(this.PortNumberTextBox, 3, 3);
            this.tableLayoutPanel1.Controls.Add(this.PortNumberLabel, 2, 3);
            this.tableLayoutPanel1.Name = "tableLayoutPanel1";
            // 
            // label1
            // 
            resources.ApplyResources(this.label1, "label1");
            this.tableLayoutPanel1.SetColumnSpan(this.label1, 4);
            this.label1.Name = "label1";
            // 
            // groupBox1
            // 
            resources.ApplyResources(this.groupBox1, "groupBox1");
            this.tableLayoutPanel1.SetColumnSpan(this.groupBox1, 4);
            this.groupBox1.Controls.Add(this.tableLayoutPanelCreds);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.TabStop = false;
            // 
            // tableLayoutPanelCreds
            // 
            resources.ApplyResources(this.tableLayoutPanelCreds, "tableLayoutPanelCreds");
            this.tableLayoutPanelCreds.Controls.Add(this.UsernameLabel, 0, 0);
            this.tableLayoutPanelCreds.Controls.Add(this.PasswordLabel, 0, 1);
            this.tableLayoutPanelCreds.Controls.Add(this.PasswordTextBox, 1, 1);
            this.tableLayoutPanelCreds.Controls.Add(this.UsernameTextBox, 1, 0);
            this.tableLayoutPanelCreds.Name = "tableLayoutPanelCreds";
            // 
            // UsernameLabel
            // 
            resources.ApplyResources(this.UsernameLabel, "UsernameLabel");
            this.UsernameLabel.Name = "UsernameLabel";
            // 
            // PasswordLabel
            // 
            resources.ApplyResources(this.PasswordLabel, "PasswordLabel");
            this.PasswordLabel.Name = "PasswordLabel";
            // 
            // PasswordTextBox
            // 
            resources.ApplyResources(this.PasswordTextBox, "PasswordTextBox");
            this.PasswordTextBox.Name = "PasswordTextBox";
            this.PasswordTextBox.UseSystemPasswordChar = true;
            // 
            // UsernameTextBox
            // 
            resources.ApplyResources(this.UsernameTextBox, "UsernameTextBox");
            this.UsernameTextBox.Name = "UsernameTextBox";
            // 
            // label2
            // 
            resources.ApplyResources(this.label2, "label2");
            this.label2.Name = "label2";
            // 
            // NamespaceLabel
            // 
            resources.ApplyResources(this.NamespaceLabel, "NamespaceLabel");
            this.NamespaceLabel.Name = "NamespaceLabel";
            // 
            // IPAddressLabel
            // 
            resources.ApplyResources(this.IPAddressLabel, "IPAddressLabel");
            this.IPAddressLabel.Name = "IPAddressLabel";
            // 
            // StorageAdapterComboBox
            // 
            this.tableLayoutPanel1.SetColumnSpan(this.StorageAdapterComboBox, 3);
            resources.ApplyResources(this.StorageAdapterComboBox, "StorageAdapterComboBox");
            this.StorageAdapterComboBox.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.StorageAdapterComboBox.FormattingEnabled = true;
            this.StorageAdapterComboBox.Name = "StorageAdapterComboBox";
            this.StorageAdapterComboBox.SelectedIndexChanged += new System.EventHandler(this.StorageAdapterComboBox_SelectedIndexChanged);
            this.StorageAdapterComboBox.DropDown += new System.EventHandler(this.StorageAdapterComboBox_DropDown);
            // 
            // NamespaceTextBox
            // 
            this.tableLayoutPanel1.SetColumnSpan(this.NamespaceTextBox, 3);
            resources.ApplyResources(this.NamespaceTextBox, "NamespaceTextBox");
            this.NamespaceTextBox.Name = "NamespaceTextBox";
            // 
            // IPAddressTextBox
            // 
            resources.ApplyResources(this.IPAddressTextBox, "IPAddressTextBox");
            this.IPAddressTextBox.Name = "IPAddressTextBox";
            // 
            // PortNumberTextBox
            // 
            resources.ApplyResources(this.PortNumberTextBox, "PortNumberTextBox");
            this.PortNumberTextBox.Name = "PortNumberTextBox";
            // 
            // PortNumberLabel
            // 
            resources.ApplyResources(this.PortNumberLabel, "PortNumberLabel");
            this.PortNumberLabel.Name = "PortNumberLabel";
            // 
            // flowLayoutPanel2
            // 
            resources.ApplyResources(this.flowLayoutPanel2, "flowLayoutPanel2");
            this.flowLayoutPanel2.Controls.Add(this.btnCancel);
            this.flowLayoutPanel2.Controls.Add(this.btnOK);
            this.flowLayoutPanel2.Name = "flowLayoutPanel2";
            // 
            // tableLayoutPanel2
            // 
            resources.ApplyResources(this.tableLayoutPanel2, "tableLayoutPanel2");
            this.tableLayoutPanel2.Controls.Add(this.flowLayoutPanel2, 0, 1);
            this.tableLayoutPanel2.Controls.Add(this.tableLayoutPanel1, 0, 0);
            this.tableLayoutPanel2.Name = "tableLayoutPanel2";
            // 
            // AddStorageLinkSystemDialog
            // 
            this.AcceptButton = this.btnOK;
            resources.ApplyResources(this, "$this");
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Dpi;
            this.CancelButton = this.btnCancel;
            this.Controls.Add(this.tableLayoutPanel2);
            this.Name = "AddStorageLinkSystemDialog";
            this.tableLayoutPanel1.ResumeLayout(false);
            this.tableLayoutPanel1.PerformLayout();
            this.groupBox1.ResumeLayout(false);
            this.groupBox1.PerformLayout();
            this.tableLayoutPanelCreds.ResumeLayout(false);
            this.tableLayoutPanelCreds.PerformLayout();
            this.flowLayoutPanel2.ResumeLayout(false);
            this.tableLayoutPanel2.ResumeLayout(false);
            this.tableLayoutPanel2.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Button btnCancel;
        private System.Windows.Forms.Button btnOK;
        private System.Windows.Forms.TableLayoutPanel tableLayoutPanel1;
        private System.Windows.Forms.FlowLayoutPanel flowLayoutPanel2;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Label NamespaceLabel;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.ComboBox StorageAdapterComboBox;
        private System.Windows.Forms.TextBox NamespaceTextBox;
        private System.Windows.Forms.TableLayoutPanel tableLayoutPanel2;
        private System.Windows.Forms.GroupBox groupBox1;
        private System.Windows.Forms.TableLayoutPanel tableLayoutPanelCreds;
        private System.Windows.Forms.Label UsernameLabel;
        private System.Windows.Forms.Label PasswordLabel;
        private System.Windows.Forms.TextBox PasswordTextBox;
        private System.Windows.Forms.TextBox UsernameTextBox;
        private System.Windows.Forms.Label IPAddressLabel;
        private System.Windows.Forms.TextBox IPAddressTextBox;
        private System.Windows.Forms.TextBox PortNumberTextBox;
        private System.Windows.Forms.Label PortNumberLabel;
    }
}