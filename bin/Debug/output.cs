using System;
namespace Reversi
{
    public class AboutDialog : System.Windows.Forms.Form
    {
        private System.Windows.Forms.PictureBox iconPictureBox;
        private System.Windows.Forms.Button okButton;
        private System.Windows.Forms.Label titleLabel;
        private System.Windows.Forms.Label versionLabel;
        private System.Windows.Forms.Label copyrightLabel;
        private System.ComponentModel.Container components;
        protected virtual void Dispose(bool disposing)
        {
            if (!(!disposing)) {
                if (!(!(@this.components))) {
                    (@this.components).Dispose();
                    goto BasicBlock_4;
                }
                else {
                    goto BasicBlock_4;
                }
            }
            BasicBlock_4:
            @this.Dispose(disposing);
        }
        private void InitializeComponent()
        {
            System.Resources.ResourceManager V_0 = (new System.Resources.ResourceManager(Type.GetTypeFromHandle((typeof(Reversi.AboutDialog).TypeHandle))));
            @this.iconPictureBox = (new System.Windows.Forms.PictureBox());
            @this.titleLabel = (new System.Windows.Forms.Label());
            @this.versionLabel = (new System.Windows.Forms.Label());
            @this.okButton = (new System.Windows.Forms.Button());
            @this.copyrightLabel = (new System.Windows.Forms.Label());
            @this.SuspendLayout();
            (@this.iconPictureBox).Location = (new System.Drawing.Point(77, 56));
            (@this.iconPictureBox).Name = "iconPictureBox";
            (@this.iconPictureBox).Size = (new System.Drawing.Size(96, 96));
            (@this.iconPictureBox).TabIndex = 0;
            (@this.iconPictureBox).TabStop = 0;
            (@this.titleLabel).AutoSize = 1;
            (@this.titleLabel).Font = (new System.Drawing.Font("Microsoft Sans Serif", 8.25f, 1, 3, 0));
            (@this.titleLabel).Location = (new System.Drawing.Point(103, 16));
            (@this.titleLabel).Name = "titleLabel";
            (@this.titleLabel).Size = (new System.Drawing.Size(44, 16));
            (@this.titleLabel).TabIndex = 0;
            (@this.titleLabel).Text = "Reversi";
            (@this.titleLabel).TextAlign = 32;
            (@this.versionLabel).AutoSize = 1;
            (@this.versionLabel).Location = (new System.Drawing.Point(95, 32));
            (@this.versionLabel).Name = "versionLabel";
            (@this.versionLabel).Size = (new System.Drawing.Size(61, 16));
            (@this.versionLabel).TabIndex = 1;
            (@this.versionLabel).Text = "Version 2.0";
            (@this.versionLabel).TextAlign = 32;
            (@this.okButton).DialogResult = 1;
            (@this.okButton).Location = (new System.Drawing.Point(88, 192));
            (@this.okButton).Name = "okButton";
            (@this.okButton).TabIndex = 3;
            (@this.okButton).Text = "OK";
            (@this.copyrightLabel).AutoSize = 1;
            (@this.copyrightLabel).Location = (new System.Drawing.Point(36, 160));
            (@this.copyrightLabel).Name = "copyrightLabel";
            (@this.copyrightLabel).Size = (new System.Drawing.Size(178, 16));
            (@this.copyrightLabel).TabIndex = 2;
            (@this.copyrightLabel).Text = "Copyright 2003-2005 by Mike Hall.";
            (@this.copyrightLabel).TextAlign = 32;
            @this.AcceptButton = (@this.okButton);
            @this.AutoScaleBaseSize = (new System.Drawing.Size(5, 13));
            @this.CancelButton = (@this.okButton);
            @this.ClientSize = (new System.Drawing.Size(250, 224));
            @this.ControlBox = 0;
            @this.Controls.Add((@this.copyrightLabel));
            @this.Controls.Add((@this.versionLabel));
            @this.Controls.Add((@this.titleLabel));
            @this.Controls.Add((@this.okButton));
            @this.Controls.Add((@this.iconPictureBox));
            @this.FormBorderStyle = 3;
            @this.Icon = ((System.Drawing.Icon)(V_0.GetObject("$this.Icon")));
            @this.MaximizeBox = 0;
            @this.MinimizeBox = 0;
            @this.Name = "AboutDialog";
            @this.ShowInTaskbar = 0;
            @this.StartPosition = 4;
            @this.Text = "About Reversi";
            @this.ResumeLayout(0);
        }
    }
}
