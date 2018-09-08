using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace sys2_xor_encrypt
{
	public partial class Form1 : Form
	{
		enum ButtonState
		{
			START,
			FILE_CHOOSED,
			ENCRYPTING,
			CANCELING
		};

		ButtonState btnState;
		ButtonState buttonState
		{
			get { return btnState; }
			set
			{
				btnState = value;
				switch (buttonState)
				{
					case ButtonState.START:
						buttonFileChoose.Invoke(new Action(()=>buttonFileChoose.Text = "Choose file"));
						break;
					case ButtonState.FILE_CHOOSED:
						buttonFileChoose.Text = $"Encrypt!";
						break;
					case ButtonState.ENCRYPTING:
						buttonFileChoose.Text = "PLEASE STOP THIS!!!";
						break;
					case ButtonState.CANCELING:
						buttonFileChoose.Text = "Canceling...";
						break;
				}
			}
		}

		FileInfo file;

		public Form1()
		{
			InitializeComponent();
			CenterToScreen();
			buttonFileChoose.Click += ButtonFileChoose_Click;
		}

		private void ButtonFileChoose_Click(object sender, EventArgs e)
		{
			switch(buttonState)
			{
				case ButtonState.START:
					firstStateClickChooseFile();
					break;
				case ButtonState.FILE_CHOOSED:
					secondStateClickToEncrypt();
					break;
				case ButtonState.ENCRYPTING:
					thirdStateClickCancel();
					break;
			}
		}
		void firstStateClickChooseFile()
		{
			OpenFileDialog fileDialog = new OpenFileDialog();
			if (fileDialog.ShowDialog() == DialogResult.OK)
			{
				file = new FileInfo(fileDialog.FileName);
				buttonState = ButtonState.FILE_CHOOSED;
				toolTip1.SetToolTip(buttonFileChoose, $"{fileDialog.FileName}");
			}
		}
		void secondStateClickToEncrypt()
		{
			if (MessageBox.Show($"Are you sure that you want to encrypt {file.Name} ?", "Confirmation", 
				MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.Yes)
			{
				buttonState = ButtonState.ENCRYPTING;
				progressBar1.Value = 0;
				Thread thread = new Thread(new ThreadStart(startEncoding));
				thread.Start();
			}
			else
			{
				buttonState = ButtonState.START;
			}
		}
		void thirdStateClickCancel()
		{
			buttonState = ButtonState.CANCELING;
		}
		void startEncoding()
		{
			using (FileStream fsSource = new FileStream(file.FullName, FileMode.Open, FileAccess.ReadWrite)) // открываем файл
			{
				int blockSize = (int)Math.Min(numericUpDown1.Value, fsSource.Length);
				byte[] bytes = new byte[blockSize];
				//progressBar1.Invoke(new Action(() =>
				//{
				//	progressBar1.Maximum = (int)fsSource.Length;
				//	progressBar1.Step = blockSize;
				//}));
				DateTime dateTime = DateTime.Now;
				int speed = 0;
				long? revers_pos = null;
				int reader_bytes;
				string key = textBoxKey.Text;
				while ((reader_bytes = fsSource.Read(bytes, 0, blockSize)) > 0)
				{
					if(revers_pos == null && buttonState == ButtonState.CANCELING)
					{
						revers_pos = fsSource.Position;
						fsSource.Position = 0;
						//progressBar1.Invoke(new Action(() =>
						//{
						//	progressBar1.Step = -blockSize;
						//}));
						continue;
					}

					if (dateTime != DateTime.Now)
					{
						dateTime = DateTime.Now;
						this.Invoke(new Action(() => toolTip1.SetToolTip(progressBar1, $"{speed/1024} Kb/s")));
						speed = 0;
					}

					fsSource.Position -= reader_bytes;
					Thread.Sleep(50); // REMOVE FOR FAST WORK
					
					for (int i = 0; i < blockSize; i++)
					{
						if (revers_pos == null && buttonState == ButtonState.CANCELING)
							break;

						if((fsSource.Position+i) % (int)(fsSource.Length/100) == 0)
							progressBar1.Invoke(new Action(() => { progressBar1.PerformStep(); }));

						bytes[i] ^= (byte)key[i % key.Length];
					//	fsSource.WriteByte(bytes[i]);
						speed++;
					}

					if (revers_pos == null && buttonState == ButtonState.CANCELING)
						continue;

					fsSource.Write(bytes, 0, reader_bytes);
					//progressBar1.Invoke(new Action(() => {
					//	progressBar1.PerformStep();
					//}));

					if (revers_pos == fsSource.Position + reader_bytes)
					{
						MessageBox.Show("Canceled");
						buttonState = ButtonState.START;
						return;
					}
				}
			}
			MessageBox.Show("File was crypted succesfully!");
			buttonState = ButtonState.START;
		}

		private void textBoxKey_TextChanged(object sender, EventArgs e)
		{
			if(textBoxKey.Text.Length < 6)
			{
				buttonFileChoose.Text = "The key must contains at least 6 symbols";
				buttonFileChoose.Enabled = false;
			}
			else
			{
				buttonFileChoose.Enabled = true;
				buttonState = buttonState;
			}
		}

		private void numericUpDown1_ValueChanged(object sender, EventArgs e)
		{
			if (numericUpDown1.Value <= 0)
			{
				buttonFileChoose.Text = "The block size must be higher then 0";
				buttonFileChoose.Enabled = false;
			}
			else
			{
				buttonFileChoose.Enabled = true;
				buttonState = buttonState;
			}
		}
	}
}
