using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.IO;                                 //для файлов
using System.Linq;
using Microsoft.JScript;
using Microsoft.JScript.Vsa;
using Microsoft;

namespace horizon
{
    public partial class Form1 : Form
    {
        int jo = 20;//шаг
        bool []flag;//объединять ли преим. напр-я

        DataGridView dgv = new DataGridView();
        string savepath;

        public Form1()
        {                      
            InitializeComponent();

            List<string> strings = new List<string>();
            string[] s_points;//данные как строки
            double[] x;
            double[] y;
            double[] x_v;//массивы для направлений
            double[] y_v;
            int kol = 0; //количество строк в файле (и эл-тов в массивах, собсна)

            /////////хш-хш

            opnfldlg.ShowDialog();
            savepath = Path.GetDirectoryName(opnfldlg.FileName);//куды потом сохранять
            string fullpath = Path.GetFullPath(opnfldlg.FileName);
            tb1.Text = fullpath;
            FileStream file = new FileStream(fullpath, FileMode.Open);
            StreamReader reader = new StreamReader(file);

            while (reader.Peek() >= 0)
            {
                strings.Add(reader.ReadLine());  //строки из файла c рез. по маркерам в strings
                kol++;
            }
            reader.Close();
            file.Close();
            kol--;//верхняя строчка с подписями

            x = new double[kol];
            y = new double[kol];

            //здесь цикл  по всем strings, типа запись в маcсивы x и у
            int q = 1;//строка с подписями не нужна
            string[] sep = { ",", " " };
            object obj;

            while (q < strings.Count)
            {
                s_points = strings[q].Split(sep, System.StringSplitOptions.RemoveEmptyEntries);
                obj = Microsoft.JScript.Eval.JScriptEvaluate(s_points[1], VsaEngine.CreateEngine());
                x[q - 1] = (double)obj;
                obj = Microsoft.JScript.Eval.JScriptEvaluate(s_points[2], VsaEngine.CreateEngine());
                y[q - 1] = (double)obj;
                q++;
            }

            //вычисления и вывод

            dgv.ColumnCount = 6;
            dgv.RowCount = kol;
            dgv.Font = new Font("Microsoft Sans Serif", 9, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
            dgv.Location = new Point(10, 40);
            dgv.BackColor = System.Drawing.Color.AntiqueWhite;
            dgv.ScrollBars = ScrollBars.Both;
            dgv.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            dgv.EditMode = DataGridViewEditMode.EditProgrammatically;
            dgv.Height = this.Height - 100;
            dgv.Width = this.Width - 20;


            Controls.Add(dgv);
            dgv.CellFormatting += new DataGridViewCellFormattingEventHandler(dgv_CellFormatting);
            dgv.CellPainting += new DataGridViewCellPaintingEventHandler(dgv_CellPainting);

            dgv.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
            dgv.ColumnHeadersDefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
            dgv.Columns[0].HeaderText = "index";
            dgv.Columns[1].HeaderText = "Амплитуды";
            dgv.Columns[2].HeaderText = "Направления";
            dgv.Columns[3].HeaderText = "Преим. напр.";
            dgv.Columns[4].HeaderText = "∑ ампл.";
            dgv.Columns[5].HeaderText = "α";

            for (int i = 0; i < kol; i++)
                dgv[0, i].Value = i;
            //амплитуд и направлений
            x_v = new double[kol / 2];//массивы направлений
            y_v = new double[kol / 2];
            double[] ampl = new double[kol / 2];//массив амплитуд
            int k = 0;
            for (int i = 0; i < kol - 1; i += 2)
            {
                dgv[1, i].Value = Math.Sqrt(Math.Pow((x[i + 1] - x[i]), 2) + Math.Pow((y[i + 1] - y[i]), 2));
                dgv[2, i].Value = "x=" + (x[i + 1] - x[i]) + ", " + "y=" + (y[i + 1] - y[i]);
                ampl[k] = Math.Sqrt(Math.Pow((x[i + 1] - x[i]), 2) + Math.Pow((y[i + 1] - y[i]), 2));
                x_v[k] = x[i + 1] - x[i];
                y_v[k] = y[i + 1] - y[i];
                k++;
            }
            //преим. напр-й
            int end = jo / 2;//по скольким парам индексов выч-ся преим. напр.
            int start = 0, ind = 0;
            double[] small_x = new double[jo / 2];//вспомогательный массив для выбора наиб. частого эл-та
            double[] small_y = new double[jo / 2];//вспомогательный массив для выбора наиб. частого эл-та
            int raz = kol / jo;
            if (kol % jo > 0) raz++;
            double[] freq_x = new double[raz];
            double[] freq_y = new double[raz];//массивы с напр-ями с наиб. частотой
            //цикл по 20-кам
            while (start < kol / 2)
            {
                for (int i = 0; i < jo / 2; i++)
                {
                    if (start + i >= kol / 2)
                        break;
                    small_x[i] = x_v[start + i];
                    small_y[i] = y_v[start + i];
                }
                var most = small_x.GroupBy(xi => xi).OrderByDescending(xi => xi.Count()).First();
                freq_x[ind] = most.Key;
                //проверяем частоту второй координаты 
                double[] here;//массив для значений из small_y, соответствующих small_x 
                int raz_here = 0;//его размерность
                if (most.Count() > 1)
                {
                    //размерность массива с y
                    for (int i = 0; i < small_x.Length; i++)
                        if (small_x[i] == most.Key)
                        {
                            raz_here++;
                        }
                    here = new double[raz_here];
                    int kuda = 0;//индекс для here
                    //заполнение сего массива
                    for (int i = 0; i < small_x.Length; i++)
                        if (small_x[i] == most.Key)
                        {
                            here[kuda] = small_y[i];
                            kuda++;
                        }
                    //находим преим. напр-е
                    var most_y = here.GroupBy(yi => yi).OrderByDescending(yi => yi.Count()).First();
                    freq_y[ind] = most_y.Key;
                }

                dgv[3, start * 2].Value = "x=" + freq_x[ind] + "," + " y=" + freq_y[ind];
                end += jo / 2;
                ind++;
                start += jo / 2;
                if ((end >= kol / 2) && (start < kol / 2))
                {
                    end = kol / 2 - 1;
                    small_x = new double[kol / 2 - start];
                    small_y = new double[kol / 2 - start];
                }
            }

            //суммы по преимущественному направлению
            int hsh = 0;//индекс для перебора преимущественных напр-й
            start = 0;
            double[] sum = new double[freq_x.Length];//суммы амплитуд по напр-ям
            while (hsh < freq_x.Length)
            {
                for (int i = start; i < start + jo / 2; i++)//ищем преимущ. напр. и соотв. ему всё на данном шаге
                {
                    if ((x_v[i] == freq_x[hsh]) && (y_v[i] == freq_y[hsh]))
                    {
                        sum[hsh] += ampl[i];
                    }
                    if (i + 1 == x_v.Length)
                        break;
                }
                //записываем найденную сумму амплитуд
                dgv[4, start * 2].Value = sum[hsh];

                hsh++;
                start += jo / 2;
            }

            //последовательно определяем участки с движением в одном напр-и и суммируем амплитуду
            hsh = 1;//индекс для перебора преимущественных напр-й
            start = 0;
            double sum2 = sum[0];//суммы амплитуд по напр-ям
            flag = new bool[freq_x.Length];
            //ind = 0;//куда прибавлять
            int par = 1;//на скока сдвигаться
            while (hsh < freq_x.Length)
            {
                if ((freq_x[hsh] == freq_x[hsh - 1]) && (freq_y[hsh] == freq_y[hsh - 1]))//след. напр. совпад. с предыдущим
                {
                    sum2 += sum[hsh];//суммируем амплитуды по совпадающему преим. напр-ю
                    par++;
                    //объединяем ячейки
                    flag[hsh] = true;
                    //записываем найденную сумму амплитуд
                    dgv[5, start].Value = sum2;
                }
                else
                {
                    sum2 = sum[hsh];
                    start += jo * par;//если не совпало, просто фигачим дальше 
                    //записываем найденную сумму амплитуд
                    dgv[5, start].Value = sum2;
                    par = 1;
                }
                hsh++;
            }             
        }

        private void button1_Click(object sender, EventArgs e)
        {            
           
        }

        private void help_Click(object sender, EventArgs e)
        {
            MessageBox.Show("Выберите файл с историей", this.Text, MessageBoxButtons.OK, MessageBoxIcon.Asterisk);
        }

        private void dgv_CellFormatting(object sender, DataGridViewCellFormattingEventArgs e)
        {
//для амплитуд  и направлений         
            if ((e.ColumnIndex == 1)||(e.ColumnIndex == 2))
            {
                if (e.RowIndex <= 0)
                    return;
                if (e.RowIndex % 2 != 0)
                {
                    e.Value = string.Empty;
                    e.FormattingApplied = true;
                }
            }
//для преобл.направлений
           if ((e.ColumnIndex >= 3)&&(e.ColumnIndex <= 5))
           {
               if (e.RowIndex <= 0)
                   return;
               if (e.RowIndex % 20 != 0)
               {
                   e.Value = string.Empty;
                   e.FormattingApplied = true;                   
               }
               else
               if (e.ColumnIndex == 5)
                   {
                       if (flag[e.RowIndex / 20] == true)
                       {
                           e.Value = string.Empty;
                           e.FormattingApplied = true;
                       }
                   }
           }
           
        }
        
        private void dgv_CellPainting(object sender, DataGridViewCellPaintingEventArgs e)
        {
//для амплитуд и направлений            
            if ((e.ColumnIndex == 1) || (e.ColumnIndex == 2))
            {
                e.AdvancedBorderStyle.Bottom = DataGridViewAdvancedCellBorderStyle.None;
                if (e.RowIndex % 2 != 0)
                    e.AdvancedBorderStyle.Top = DataGridViewAdvancedCellBorderStyle.None;
                 else 
                    e.AdvancedBorderStyle.Top = DataGridViewAdvancedCellBorderStyle.Single;
            }
//для преобл. направлений
            if ((e.ColumnIndex >= 3) && (e.ColumnIndex <= 5))
            {
                e.AdvancedBorderStyle.Bottom = DataGridViewAdvancedCellBorderStyle.None;
                if (e.RowIndex % 20 != 0)
                    e.AdvancedBorderStyle.Top = DataGridViewAdvancedCellBorderStyle.None;
                else
                    if ((flag[e.RowIndex / 20] == true) && (e.ColumnIndex == 5))
                        e.AdvancedBorderStyle.Top = DataGridViewAdvancedCellBorderStyle.None;
                    else
                        e.AdvancedBorderStyle.Top = DataGridViewAdvancedCellBorderStyle.Single;
            }
            
        }

        private void button2_Click(object sender, EventArgs e)
        {
            string s = "history";
            int ind = savepath.IndexOf(s) + 8;
            s = savepath.Substring(ind);
            ind = s.IndexOf('_');
            int ind2 = s.IndexOf('.');
            string s2 = s.Substring(ind + 1, ind2 - ind - 1);
            s = s.Remove(ind);

            File.Delete(savepath + "\\" + s + "_horizon_" + s2 + ".csv");
            var sw = new StreamWriter(savepath + "\\" + s + "_horizon_" + s2 + ".csv", true, Encoding.UTF8);

            foreach (DataGridViewColumn column in dgv.Columns)//записываем шапку
            {
                sw.Write(column.HeaderText + ";");
            }
            sw.WriteLine();

            foreach (DataGridViewRow row in dgv.Rows)
                if (!row.IsNewRow)
                {
                    var first = true;
                    foreach (DataGridViewCell cell in row.Cells)
                    {
                        if (!first) sw.Write(";");
                        if(cell.Value!=null)
                        sw.Write(cell.Value.ToString());
                        else sw.Write(";");
                        first = false;
                    }
                    sw.WriteLine();
                }
            sw.Close(); 
        }
    }
}
//усё!

//opnfldlg.SafeFileName - имя ф-ла с расширением
// MessageBox.Show(s_points[0], "", MessageBoxButtons.OK, MessageBoxIcon.Asterisk);

//foreach (String str in s_points)
//    tb1.Text += str + Environment.NewLine;

//points[q-1,i-1] = Convert.ToDouble(s_points[i]);

//просто вывод rezults в текстбокс
//int k = 1;
//for (int i = 0; i < (n - 2) / 2; i++)
//{
//    tb2.Text += "di" + k.ToString() + "  " + "de" + k.ToString() + "  ";
//    k++;
//}            
//tb2.Text += "abe  cam  №т." + Environment.NewLine;
//for (int i = 0; i < raz; i++)
//{
//    for (int j = 0; j < n + 1; j++)
//        tb2.Text += rezults[i, j].ToString()+"      ";
//    tb2.Text += Environment.NewLine;
//}

//int hey=x_v[start];//с чем сравниваем
//               int sum=0;//скока раз встречается
//               for (int i = start+1; i < jo; i++)
//               {
//                   if (x_v[i] == hey)
//                       sum++;
//               }

//if (flag2 == 0)//если первый раз встречается значение в small_y
//                                {
//                                    pred = small_y[i];//тогда сравнивать с ним
//                                    flag2++;
//                                }

 //if (i != here)
 //                           {
 //                               if (small_y[i] == pred)
 //                                   freq_y[ind] = small_y[i];
 //                               else
 //                               {

 //                               }
 //                           }
 //                           else
 //                           {
                                
 //                           }         

//double pred=0;
//                    int here = 0;//индекс того, что из small_y записано в pred для последующего сравнения
////назначение значения pred                    
//                    for (int i = 0; i < small_y.Length; i++)
//                    {
//                        if (small_x[i] == most.Key)
//                        {
//                            pred = small_y[i];
//                            here = i;
//                            break;
//                        }
//                    }