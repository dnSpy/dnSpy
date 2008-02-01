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
            System.Resources.ResourceManager V_0 = (IL__newobj(.ctor(), Type.GetTypeFromHandle((IL__ldtoken(Reversi.AboutDialog)))));
            IL__stfld(iconPictureBox, @this, (IL__newobj(.ctor())));
            IL__stfld(titleLabel, @this, (IL__newobj(.ctor())));
            IL__stfld(versionLabel, @this, (IL__newobj(.ctor())));
            IL__stfld(okButton, @this, (IL__newobj(.ctor())));
            IL__stfld(copyrightLabel, @this, (IL__newobj(.ctor())));
            @this.SuspendLayout();
            (@this.iconPictureBox).set_Location((IL__newobj(.ctor(), 77, 56)));
            (@this.iconPictureBox).set_Name("iconPictureBox");
            (@this.iconPictureBox).set_Size((IL__newobj(.ctor(), 96, 96)));
            (@this.iconPictureBox).set_TabIndex(0);
            (@this.iconPictureBox).set_TabStop(0);
            (@this.titleLabel).set_AutoSize(1);
            (@this.titleLabel).set_Font((IL__newobj(.ctor(), "Microsoft Sans Serif", 8.25f, 1, 3, 0)));
            (@this.titleLabel).set_Location((IL__newobj(.ctor(), 103, 16)));
            (@this.titleLabel).set_Name("titleLabel");
            (@this.titleLabel).set_Size((IL__newobj(.ctor(), 44, 16)));
            (@this.titleLabel).set_TabIndex(0);
            (@this.titleLabel).set_Text("Reversi");
            (@this.titleLabel).set_TextAlign(32);
            (@this.versionLabel).set_AutoSize(1);
            (@this.versionLabel).set_Location((IL__newobj(.ctor(), 95, 32)));
            (@this.versionLabel).set_Name("versionLabel");
            (@this.versionLabel).set_Size((IL__newobj(.ctor(), 61, 16)));
            (@this.versionLabel).set_TabIndex(1);
            (@this.versionLabel).set_Text("Version 2.0");
            (@this.versionLabel).set_TextAlign(32);
            (@this.okButton).set_DialogResult(1);
            (@this.okButton).set_Location((IL__newobj(.ctor(), 88, 192)));
            (@this.okButton).set_Name("okButton");
            (@this.okButton).set_TabIndex(3);
            (@this.okButton).set_Text("OK");
            (@this.copyrightLabel).set_AutoSize(1);
            (@this.copyrightLabel).set_Location((IL__newobj(.ctor(), 36, 160)));
            (@this.copyrightLabel).set_Name("copyrightLabel");
            (@this.copyrightLabel).set_Size((IL__newobj(.ctor(), 178, 16)));
            (@this.copyrightLabel).set_TabIndex(2);
            (@this.copyrightLabel).set_Text("Copyright 2003-2005 by Mike Hall.");
            (@this.copyrightLabel).set_TextAlign(32);
            @this.set_AcceptButton((@this.okButton));
            @this.set_AutoScaleBaseSize((IL__newobj(.ctor(), 5, 13)));
            @this.set_CancelButton((@this.okButton));
            @this.set_ClientSize((IL__newobj(.ctor(), 250, 224)));
            @this.set_ControlBox(0);
            @this.get_Controls().Add((@this.copyrightLabel));
            @this.get_Controls().Add((@this.versionLabel));
            @this.get_Controls().Add((@this.titleLabel));
            @this.get_Controls().Add((@this.okButton));
            @this.get_Controls().Add((@this.iconPictureBox));
            @this.set_FormBorderStyle(3);
            @this.set_Icon((IL__castclass(System.Drawing.Icon, (V_0.GetObject("$this.Icon")))));
            @this.set_MaximizeBox(0);
            @this.set_MinimizeBox(0);
            @this.set_Name("AboutDialog");
            @this.set_ShowInTaskbar(0);
            @this.set_StartPosition(4);
            @this.set_Text("About Reversi");
            @this.ResumeLayout(0);
        }
    }
}
