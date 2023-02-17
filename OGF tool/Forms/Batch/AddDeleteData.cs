﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace OGF_tool
{
    public partial class AddDeleteData : Form
    {
        private string folder;
        private OGF_Editor Editor;
        private Batch.BatchChunks Chunk;
        private bool delete;
        public AddDeleteData(OGF_Editor editor, Batch batch, Batch.BatchChunks chunk, string ogf_folder_path, bool delete)
        {
            InitializeComponent();
            folder = ogf_folder_path;
            Editor = editor;
            Chunk = chunk;
            this.delete = delete;

            if (Chunk == Batch.BatchChunks.Lod)
                DataTextBox.Multiline = false;

            if (delete)
            {
                StartButton.Size = new Size(89, 74);
                CreateChunkChbx.Visible = false;
            }

            Text = (delete ? "Delete " : "Add ") + Batch.Capitalize(chunk.ToString()) + (delete ? " (use \"$all\" for delete all chunk)" : "");

            BoxTextChanged(DataTextBox, null);
        }

        private void StartButton_Click(object sender, EventArgs e)
        {
            if (OGF_Editor.IsTextCorrect(DataTextBox.Text))
            {
                uint FilesCount = 0;
                uint LinesCount = 0;
                string[] files = Directory.GetFiles(folder, "*.ogf", SearchOption.AllDirectories);

                for (int i = 0; i < files.Length; i++)
                {
                    OGF_Model OGF;
                    byte[] OGF_data;
                    bool save = false;
                    if (Editor.OpenFile(files[i], out OGF, out OGF_data, true))
                    {
                        foreach (string line in DataTextBox.Lines)
                        {
                            if ((delete ? Batch.ProcessDelete(OGF, Chunk, line, ref LinesCount)  : Batch.ProcessAdd(OGF, Chunk, line, ref LinesCount, CreateChunkChbx.Checked)))
                                save = true;
                        }

                        if (save)
                        {
                            Editor.SaveFile(files[i], OGF, OGF_data);
                            FilesCount++;
                        }
                    }
                }
                MessageBox.Show($"{LinesCount} lines {(delete ? "deleted" : "added")} in {FilesCount} files!", "Batch Info", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            else
                AutoClosingMessageBox.Show("Please, fill in the data field.", "Error", 1500, MessageBoxIcon.Error);
        }

        private void BoxTextChanged(object sender, EventArgs e)
        {
            RichTextBox textBox = sender as RichTextBox;

            if (OGF_Editor.IsTextCorrect(textBox.Text))
                textBox.BackColor = SystemColors.Window;
            else
                textBox.BackColor = Color.FromArgb(255, 255, 128, 128);
        }
    }
}