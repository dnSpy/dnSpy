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
                    IL__callvirt(Dispose(), (@this.components));
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
            IL__callvirt(set_Location(), (@this.iconPictureBox), (IL__newobj(.ctor(), 77, 56)));
            IL__callvirt(set_Name(), (@this.iconPictureBox), "iconPictureBox");
            IL__callvirt(set_Size(), (@this.iconPictureBox), (IL__newobj(.ctor(), 96, 96)));
            IL__callvirt(set_TabIndex(), (@this.iconPictureBox), 0);
            IL__callvirt(set_TabStop(), (@this.iconPictureBox), 0);
            IL__callvirt(set_AutoSize(), (@this.titleLabel), 1);
            IL__callvirt(set_Font(), (@this.titleLabel), (IL__newobj(.ctor(), "Microsoft Sans Serif", 8.25f, 1, 3, 0)));
            IL__callvirt(set_Location(), (@this.titleLabel), (IL__newobj(.ctor(), 103, 16)));
            IL__callvirt(set_Name(), (@this.titleLabel), "titleLabel");
            IL__callvirt(set_Size(), (@this.titleLabel), (IL__newobj(.ctor(), 44, 16)));
            IL__callvirt(set_TabIndex(), (@this.titleLabel), 0);
            IL__callvirt(set_Text(), (@this.titleLabel), "Reversi");
            IL__callvirt(set_TextAlign(), (@this.titleLabel), 32);
            IL__callvirt(set_AutoSize(), (@this.versionLabel), 1);
            IL__callvirt(set_Location(), (@this.versionLabel), (IL__newobj(.ctor(), 95, 32)));
            IL__callvirt(set_Name(), (@this.versionLabel), "versionLabel");
            IL__callvirt(set_Size(), (@this.versionLabel), (IL__newobj(.ctor(), 61, 16)));
            IL__callvirt(set_TabIndex(), (@this.versionLabel), 1);
            IL__callvirt(set_Text(), (@this.versionLabel), "Version 2.0");
            IL__callvirt(set_TextAlign(), (@this.versionLabel), 32);
            IL__callvirt(set_DialogResult(), (@this.okButton), 1);
            IL__callvirt(set_Location(), (@this.okButton), (IL__newobj(.ctor(), 88, 192)));
            IL__callvirt(set_Name(), (@this.okButton), "okButton");
            IL__callvirt(set_TabIndex(), (@this.okButton), 3);
            IL__callvirt(set_Text(), (@this.okButton), "OK");
            IL__callvirt(set_AutoSize(), (@this.copyrightLabel), 1);
            IL__callvirt(set_Location(), (@this.copyrightLabel), (IL__newobj(.ctor(), 36, 160)));
            IL__callvirt(set_Name(), (@this.copyrightLabel), "copyrightLabel");
            IL__callvirt(set_Size(), (@this.copyrightLabel), (IL__newobj(.ctor(), 178, 16)));
            IL__callvirt(set_TabIndex(), (@this.copyrightLabel), 2);
            IL__callvirt(set_Text(), (@this.copyrightLabel), "Copyright 2003-2005 by Mike Hall.");
            IL__callvirt(set_TextAlign(), (@this.copyrightLabel), 32);
            @this.set_AcceptButton((@this.okButton));
            IL__callvirt(set_AutoScaleBaseSize(), @this, (IL__newobj(.ctor(), 5, 13)));
            @this.set_CancelButton((@this.okButton));
            @this.set_ClientSize((IL__newobj(.ctor(), 250, 224)));
            @this.set_ControlBox(0);
            IL__callvirt(Add(), @this.get_Controls(), (@this.copyrightLabel));
            IL__callvirt(Add(), @this.get_Controls(), (@this.versionLabel));
            IL__callvirt(Add(), @this.get_Controls(), (@this.titleLabel));
            IL__callvirt(Add(), @this.get_Controls(), (@this.okButton));
            IL__callvirt(Add(), @this.get_Controls(), (@this.iconPictureBox));
            @this.set_FormBorderStyle(3);
            @this.set_Icon((IL__castclass(System.Drawing.Icon, (IL__callvirt(GetObject(), V_0, "$this.Icon")))));
            @this.set_MaximizeBox(0);
            @this.set_MinimizeBox(0);
            @this.set_Name("AboutDialog");
            @this.set_ShowInTaskbar(0);
            @this.set_StartPosition(4);
            IL__callvirt(set_Text(), @this, "About Reversi");
            @this.ResumeLayout(0);
        }
    }
}
