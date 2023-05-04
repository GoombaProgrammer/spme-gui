using System.IO.Compression;
using System.Text;

namespace spme_gui
{
    public partial class Form1 : Form
    {
        string inputModel = "";
        string inputModel2 = "";
        string outputModel = "";
        string outputModel2 = "";

        public Form1()
        {
            InitializeComponent();
        }
        public byte[] parse(byte[] sentencePieceModel, string find, string change)
        {
            // Loop until find is found in model
            for (int i = 0; i < sentencePieceModel.Length; i++)
            {
                if (sentencePieceModel[i] == System.Text.Encoding.UTF8.GetBytes(find)[0])
                {
                    bool found = true;
                    for (int j = 0; j < System.Text.Encoding.UTF8.GetBytes(find).Length; j++)
                    {
                        if (sentencePieceModel[i + j] != System.Text.Encoding.UTF8.GetBytes(find)[j])
                        {
                            found = false;
                            break;
                        }
                    }
                    if (found)
                    {
                        if (sentencePieceModel[i - 4] == System.Text.Encoding.UTF8.GetBytes(find).Length + 3)
                        {
                            Console.WriteLine("Found at: " + i);
                            // While change is found, add a null byte to the end of change
                            while (true)
                            {
                                bool found2 = true;
                                for (int j = 0; j < System.Text.Encoding.UTF8.GetBytes(change).Length; j++)
                                {
                                    if (sentencePieceModel[i + j] != System.Text.Encoding.UTF8.GetBytes(change)[j])
                                    {
                                        found2 = false;
                                        break;
                                    }
                                }
                                if (found2)
                                {
                                    change += "\0";
                                }
                                else
                                {
                                    break;
                                }
                            }
                            // Add to array if length is different
                            if (System.Text.Encoding.UTF8.GetBytes(change).Length > System.Text.Encoding.UTF8.GetBytes(find).Length)
                            {
                                List<byte> l = sentencePieceModel.ToList();
                                l.InsertRange(i + System.Text.Encoding.UTF8.GetBytes(find).Length, new byte[System.Text.Encoding.UTF8.GetBytes(change).Length - System.Text.Encoding.UTF8.GetBytes(find).Length]);
                                sentencePieceModel = l.ToArray();
                            }
                            // Change
                            for (int j = 0; j < System.Text.Encoding.UTF8.GetBytes(change).Length; j++)
                            {
                                sentencePieceModel[i + j] = System.Text.Encoding.UTF8.GetBytes(change)[j];
                            }
                            // Remove from array if length is different
                            if (System.Text.Encoding.UTF8.GetBytes(change).Length < System.Text.Encoding.UTF8.GetBytes(find).Length)
                            {
                                List<byte> l = sentencePieceModel.ToList();
                                l.RemoveRange(i + System.Text.Encoding.UTF8.GetBytes(change).Length, System.Text.Encoding.UTF8.GetBytes(find).Length - System.Text.Encoding.UTF8.GetBytes(change).Length);
                                sentencePieceModel = l.ToArray();
                            }
                            sentencePieceModel[i - 4] = (byte)(System.Text.Encoding.UTF8.GetBytes(change).Length + 3);
                            sentencePieceModel[i - 6] = (byte)(System.Text.Encoding.UTF8.GetBytes(change).Length + 10);
                            break;
                        }
                        else
                        {
                            continue;
                        }
                    }
                }
            }
            return sentencePieceModel;
        }

        private void button1_Click(object sender, EventArgs e)
        {
            string tempDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            string tempDir2 = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            // Unzip model file
            Directory.CreateDirectory(tempDir);
            Directory.CreateDirectory(tempDir2);
            ZipFile.ExtractToDirectory(inputModel, tempDir, true);
            ZipFile.ExtractToDirectory(inputModel2, tempDir2, true);
            // Move files from each folder to root
            foreach (string dir in Directory.GetDirectories(tempDir))
            {
                foreach (string file in Directory.GetFiles(dir))
                {
                    File.Move(file, Path.Combine(tempDir, Path.GetFileName(file)));
                }
                foreach (string subDir in Directory.GetDirectories(dir))
                {
                    Directory.Move(subDir, Path.Combine(tempDir, Path.GetFileName(subDir)));
                }
                Directory.Delete(dir);
            }
            foreach (string dir in Directory.GetDirectories(tempDir2))
            {
                foreach (string file in Directory.GetFiles(dir))
                {
                    File.Move(file, Path.Combine(tempDir2, Path.GetFileName(file)));
                }
                foreach (string subDir in Directory.GetDirectories(dir))
                {
                    Directory.Move(subDir, Path.Combine(tempDir2, Path.GetFileName(subDir)));
                }
                Directory.Delete(dir);
            }
            byte[] sentencePieceModel = System.IO.File.ReadAllBytes(tempDir + "\\sentencepiece.model");
            string find;
            string change;
            find = textBox1.Text;
            change = textBox2.Text;
            byte[] bytes = Encoding.Default.GetBytes(find);
            find = Encoding.UTF8.GetString(bytes);
            bytes = Encoding.Default.GetBytes(change);
            change = Encoding.UTF8.GetString(bytes);
            sentencePieceModel = parse(sentencePieceModel, find, change);
            byte[] sentencePieceModel2 = System.IO.File.ReadAllBytes(tempDir2 + "\\sentencepiece.model");
            sentencePieceModel2 = parse(sentencePieceModel2, find, change);
            // Read model\shared_vocabulary.txt
            string sharedVocabulary = File.ReadAllText(tempDir + "\\model\\shared_vocabulary.txt", System.Text.Encoding.UTF8);
            string sharedVocabulary2 = File.ReadAllText(tempDir2 + "\\model\\shared_vocabulary.txt", System.Text.Encoding.UTF8);
            // Change first occurence of find (so we can't use .Replace)
            int index = sharedVocabulary.IndexOf(find);
            if (index > 0)
            {
                sharedVocabulary = sharedVocabulary.Substring(0, index) + change + sharedVocabulary.Substring(index + find.Length);
            }
            // Change first occurence of find (so we can't use .Replace)
            int index2 = sharedVocabulary2.IndexOf(find);
            if (index2 > 0)
            {
                sharedVocabulary2 = sharedVocabulary2.Substring(0, index2) + change + sharedVocabulary2.Substring(index2 + find.Length);
            }
            // Write model\shared_vocabulary.txt
            File.WriteAllText(tempDir + "\\model\\shared_vocabulary.txt", sharedVocabulary);
            File.WriteAllText(tempDir2 + "\\model\\shared_vocabulary.txt", sharedVocabulary2);
            // Write output
            File.WriteAllBytes(tempDir + "\\sentencepiece.model", sentencePieceModel);
            File.WriteAllBytes(tempDir2 + "\\sentencepiece.model", sentencePieceModel2);
            File.Delete(outputModel);
            File.Delete(outputModel2);
            ZipFile.CreateFromDirectory(tempDir, outputModel, CompressionLevel.Optimal, true);
            ZipFile.CreateFromDirectory(tempDir2, outputModel2, CompressionLevel.Optimal, true);
            // Delete temp folders
            Directory.Delete(tempDir, true);
            Directory.Delete(tempDir2, true);
            // Show message
            MessageBox.Show("Done!");
        }

        private void Form1_Load(object sender, EventArgs e)
        {

        }

        private void button2_Click(object sender, EventArgs e)
        {
            SaveFileDialog saveFileDialog1 = new SaveFileDialog();
            saveFileDialog1.Filter = "Argos model|*.argosmodel";
            saveFileDialog1.Title = "Save an Argos model (lang1-lang2)";
            saveFileDialog1.ShowDialog();
            if (saveFileDialog1.FileName != "")
            {
                outputModel = saveFileDialog1.FileName;
                SaveFileDialog saveFileDialog2 = new SaveFileDialog();
                saveFileDialog2.Filter = "Argos model|*.argosmodel";
                saveFileDialog2.Title = "Save an Argos model (lang2-lang1)";
                saveFileDialog2.ShowDialog();
                if (saveFileDialog2.FileName != "")
                {
                    outputModel2 = saveFileDialog2.FileName;
                    button1.Enabled = true;
                }
            }
        }

        private void setInputFile_Click(object sender, EventArgs e)
        {
            OpenFileDialog openFileDialog1 = new OpenFileDialog();
            openFileDialog1.Filter = "Argos model|*.argosmodel";
            openFileDialog1.Title = "Select an Argos model (lang1-lang2)";
            openFileDialog1.ShowDialog();
            if (openFileDialog1.FileName != "")
            {
                inputModel = openFileDialog1.FileName;
                OpenFileDialog openFileDialog2 = new OpenFileDialog();
                openFileDialog2.Filter = "Argos model|*.argosmodel";
                openFileDialog2.Title = "Select an Argos model (lang2-lang1)";
                openFileDialog2.ShowDialog();
                if (openFileDialog2.FileName != "")
                {
                    inputModel2 = openFileDialog2.FileName;
                    button2.Enabled = true;
                }
            }
        }
    }
}