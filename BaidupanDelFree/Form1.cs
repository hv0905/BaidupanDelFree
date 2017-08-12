using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;
using System.Windows.Forms;

namespace BaidupanDelFree
{
    public partial class Form1 : Form
    {

        public List<string> _files = new List<string>();
        private byte[] _passcode;
        private uint _nullBytCount;
        private Thread _workThread;
        private bool _running;

        public Form1()
        {
            InitializeComponent();
            CheckForIllegalCrossThreadCalls = false;
        }

        private void tableLayoutPanel1_Paint(object sender, PaintEventArgs e)
        {

        }

        private void button4_Click(object sender, EventArgs e)
        {
            if(MessageBox.Show("确定要清空所有项目吗？","Warning",MessageBoxButtons.YesNo,MessageBoxIcon.Warning,MessageBoxDefaultButton.Button2) == DialogResult.No) return;
            _files.Clear();
            listBox1.Items.Clear();
        }

        private void button3_Click(object sender, EventArgs e)
        {
            
            int i = listBox1.SelectedIndex;
            if(i == -1) return;
            _files.RemoveAt(listBox1.SelectedIndex);
            listBox1.Items.RemoveAt(listBox1.SelectedIndex);
            listBox1.SelectedIndex = i-1;
        }

        private void button2_Click(object sender, EventArgs e)
        {
            var fbd = new FolderBrowserDialog
            {
                Description = "选择目录",
                ShowNewFolderButton = true
            };
            if (fbd.ShowDialog() == DialogResult.OK) {
                AddDir(fbd.SelectedPath);
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            var ofd = new OpenFileDialog
            {
                Filter = "所有文件|*.*",
                AddExtension = false,
                CheckFileExists = true,
                CheckPathExists = true,
                Multiselect = true,
            };
            if (ofd.ShowDialog() == DialogResult.OK) {
                AddFiles(ofd.FileNames);
            }

        }

        private void button6_Click(object sender, EventArgs e) => MessageBox.Show(
                "原理：\n本程序是通过在文件末端增加随机字节（或指定字节）来更改文件的sha-1值，从而使百度的文件检测机制无法奏效。这对大多数已知文件类型（包括所有类型的视频文件）不会产生影响，但是对于部分文件类型（例如txt文本文档）会导致末端出现奇怪字符。\n制作：HV0905 Studio\n官方网站：hv0905.github.io\n喜欢就请给我们一个like，或者捐助一下\n最后，祝使用愉快q(≧▽≦q)\n                                                    ---HIM 2017", "关于");

        private void Form1_Load(object sender, EventArgs e)
        {

        }

        private void button7_Click(object sender, EventArgs e)
        {
            var fbd = new FolderBrowserDialog
            {
                Description = "选择保存位置",
                ShowNewFolderButton = true
            };
            if (fbd.ShowDialog() == DialogResult.OK) {
                textBox3.Text = fbd.SelectedPath;
            }
        }

        private void listBox1_DragEnter(object sender, DragEventArgs e)
        {
            if(e.Data.GetDataPresent(DataFormats.FileDrop,true) || e.Data.GetDataPresent(DataFormats.Locale, true))
            e.Effect = DragDropEffects.Copy;
            else e.Effect = DragDropEffects.None;
        }

        private void listBox1_DragDrop(object sender, DragEventArgs e)
        {
            //foreach (var item in e.Data.GetFormats(true)) {
            //    Debug.WriteLine(item);
            //    if (item. DataFormats.FileDrop)
            //    {
            //        AddFiles(e.Data.GetData(DataFormats.FileDrop) as string);
            //    }
            //}
            AddFiles(((string[])e.Data.GetData(DataFormats.FileDrop,true)));
            AddDir(((string[])e.Data.GetData(DataFormats.FileDrop,true)));

        }


        private void AddDir(params string[] path)
        {
            foreach (var item in path) {
                if(!Directory.Exists(item)) continue;
                var childDir = new List<string> {item};
                for (int i = 0; i < childDir.Count; i++)
                {
                    try
                    {
                        childDir.AddRange(Directory.GetDirectories(childDir[i]));
                    } catch { }
                }
                var files = new List<string>();
                foreach (var item_ in childDir)
                {
                    try
                    {
                        files.AddRange(Directory.GetFiles(item_));
                    } catch { }
                }
                AddFiles(files.ToArray());
            }
        }

        private void AddFiles(params string[] files)
        {
            foreach (var item in files) {
                if(!File.Exists(item)) continue;
                if(_files.Exists(a => a == item)) continue;
                listBox1.Items.Add(item);
                _files.Add(item);
            }
        }

        private void button5_Click(object sender, EventArgs e)
        {
            if (_running)
            {
                _workThread.Abort();
                _running = false;
                button1.Enabled = button2.Enabled = button3.Enabled = button4.Enabled = button7.Enabled
                    = numericUpDown1.Enabled = radioButton1.Enabled = radioButton2.Enabled = true;
                textBox2.ReadOnly = textBox3.ReadOnly = false;
                button5.Text = "开始";
                //remove items
                for (int i = 0; i < listBox1.Items.Count; i++)
                {
                    if (((string) listBox1.Items[i]).StartsWith("[进行中]"))
                    {
                        listBox1.Items[i] = ((string)listBox1.Items[i]).Substring(5);
                        continue;
                    }
                    if (!((string)listBox1.Items[i]).StartsWith("[已完成]")) continue;
                    _files.RemoveAt(i);
                    listBox1.Items.RemoveAt(i);
                    i--;
                }
            }
            else
            {
                if (listBox1.Items.Count == 0)
                {
                    MessageBox.Show("任务列表中无项目", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }
                if ((!radioButton1.Checked) && (!Directory.Exists(textBox3.Text)))
                {
                    MessageBox.Show("存储路径不存在！", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }
                _passcode = string.IsNullOrEmpty(textBox2.Text)
                    ?Guid.NewGuid().ToByteArray()
                    : Encoding.UTF8.GetBytes(textBox2.Text);
                _nullBytCount = (uint) numericUpDown1.Value;
                button1.Enabled = button2.Enabled = button3.Enabled = button4.Enabled = button7.Enabled
                    = numericUpDown1.Enabled = radioButton1.Enabled = radioButton2.Enabled = false;
                textBox2.ReadOnly = textBox3.ReadOnly = true;
                progressBar1.Value = 0;
                progressBar1.Maximum = _files.Count;
                button5.Text = "停止";
                _workThread = new Thread(RunWork);
                _workThread.Start();
                _running = true;
            }
        }


        private void RunWork()
        {
            textBox1.AppendText("开始sha-1混淆\r\n");
            for (int i = 0; i < _files.Count; i++)
            {
                try
                {
                    textBox1.AppendText($"[{i}]{_files[i]}正在混淆.\r\n");
                    listBox1.Items[i] = "[进行中]" + _files[i];
                    var source = File.Open(_files[i], FileMode.Open);
                    var target =
                        File.Open(
                            radioButton1.Checked
                                ? Path.Combine(Path.GetDirectoryName(_files[i]),
                                    Path.GetFileNameWithoutExtension(_files[i]) + " - 反和谐" +
                                    Path.GetExtension(_files[i]))
                                : Path.Combine(textBox3.Text, Path.GetFileName(_files[i])), FileMode.Create);
                    source.CopyTo(target);
                    source.Close();
                    target.Write(new byte[_nullBytCount],0,(int)_nullBytCount );
                    target.Write(_passcode,0,_passcode.Length);
                    target.Flush();
                    target.Close();
                    textBox1.AppendText($"[{i}]{_files[i]}混淆成功.\r\n");
                    listBox1.Items[i] = "[已完成]" + _files[i];
                    listBox1.SelectedIndex = i;
                }
                catch
                {
                    textBox1.AppendText($"[{i}]{_files[i]}读取错误，跳过。\r\n");
                }
                progressBar1.Value++;
            }
            textBox1.AppendText("所有操作完成。\r\n");
            button1.Enabled = button2.Enabled = button3.Enabled = button4.Enabled = button7.Enabled
                = numericUpDown1.Enabled = radioButton1.Enabled = radioButton2.Enabled = true;
            textBox2.ReadOnly = textBox3.ReadOnly = false;
            button5.Text = "开始";
            _files.Clear();
            listBox1.Items.Clear();
            _running = false;
            if (checkBox1.Checked)
                Process.Start("shutdown", "-s -t 30");
        }
    }
}
