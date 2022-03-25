using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;
using System.Windows.Forms;
using System.ComponentModel;
using System.Collections;
using System.IO;

namespace SteganosGrapho
{
    class ProcessBytes
    {
        public static long text_size = 0;
        byte[] input_text_byte_list;
        string label_start = "St@rT";
        string label_end = "4IniSh";
        string strMessage;

        public Bitmap MakeLSB(Bitmap src, ref BackgroundWorker worker)
        {
            Bitmap res = new Bitmap(src.Width, src.Height);
            for (int i = 0; i < src.Width; i++)
            {
                worker.ReportProgress((int)((float)i / src.Width * 100));
                if (worker.CancellationPending)
                    return null;
                for (int j = 0; j < src.Height; j++)
                {
                    Color pixelColor = src.GetPixel(i, j);
                    BitArray colorArray = Byte2Bit(pixelColor.R);
                    colorArray[3] = false; // т.е. 0
                    colorArray[4] = false;
                    colorArray[5] = false;
                    colorArray[6] = false;
                    colorArray[7] = false;
                    byte newR = Bit2Byte(colorArray);
                    colorArray = Byte2Bit(pixelColor.G);
                    colorArray[3] = false;
                    colorArray[4] = false;
                    colorArray[5] = false;
                    colorArray[6] = false;
                    colorArray[7] = false;
                    byte newG = Bit2Byte(colorArray);
                    colorArray = Byte2Bit(pixelColor.B);
                    colorArray[3] = false;
                    colorArray[4] = false;
                    colorArray[5] = false;
                    colorArray[6] = false;
                    colorArray[7] = false;
                    byte newB = Bit2Byte(colorArray);
                    Color newColor = Color.FromArgb(newR, newG, newB);
                    res.SetPixel(i, j, newColor);
                }
            }
            return res;
        }

        private BitArray Byte2Bit(byte src)
        {
            BitArray bitArray = new BitArray(8);
            bool bit = false;
            for (int i = 0; i < 8; i++)
            {
                if ((src >> i & 1) == 1)
                {
                    bit = true;
                }
                else bit = false;
                bitArray[i] = bit;
            }
            return bitArray;
        }
        private byte Bit2Byte(BitArray src)
        {
            byte value = 0;
            for (int i = 0; i < src.Count; i++)
                if (src[i] == true)
                    value += (byte)Math.Pow(2, i);
            return value;
        }
        public int Clamp(int value, int min, int max)
        {
            if (value < min)
                return min;
            if (value > max)
                return max;
            return value;
        }

        public Bitmap EncodeRawImage(Bitmap src, ref BackgroundWorker worker)
        {
            ReadText(ref worker);
            if (input_text_byte_list == null)
            {
                Form1.ShowMsgBox("List is empty!");
                return src;
            }
            Bitmap res = new Bitmap(src);
            int i = 0, j = 0;
            // write text into image
            bool stop = false;
            int text_ind = 0;
            for (; i < src.Width; i++)
               {
                worker.ReportProgress(Clamp((int)((float)i / src.Width * 50 + 50),
                0, 100));
                if (worker.CancellationPending)
                    return null;
                for (j = 0; j < src.Height; j++)
                {
                    if (text_ind == text_size)
                    {
                        stop = true;
                        break;
                    }
                    EncodeSymbol(ref src, ref res, ref i, ref j, ref text_ind,
                    input_text_byte_list[text_ind]);
                }
                if (stop) break;
            }
            return res;
        }

        private void ReadText(ref BackgroundWorker worker)
        {
            string text_file = Form1.input_file_name();
            if (!File.Exists(text_file))
            {
                Form1.ShowMsgBox("File is empty!");
                return;
            }
            int input_text_byte_list_ind = 0;
            FileStream fs = File.Open(text_file, FileMode.Open, FileAccess.ReadWrite);
            FileInfo finfo = new FileInfo(text_file);
            text_size = finfo.Length;
            if (text_size > Form1.img_size())
            {
                Form1.ShowMsgBox("Picture is too small for encoding!");
                return;
            }
            fs.SetLength(text_size);
            input_text_byte_list = new byte[text_size];
            for (int w = 0; w < fs.Length; w++)
            {
                worker.ReportProgress((int)((float)w / fs.Length * 50));
                if (worker.CancellationPending)
                    return;
                input_text_byte_list[input_text_byte_list_ind] =
                Convert.ToByte(fs.ReadByte());
                input_text_byte_list_ind++;
            }
            fs.Close();
        }

        private void EncodeSymbol(ref Bitmap src, ref Bitmap res,
                ref int i, ref int j, ref int index, byte byte_)
        {
            Color pixelColor = src.GetPixel(i, j);
            BitArray colorArray = Byte2Bit(pixelColor.R);
            BitArray messageArray = Byte2Bit(byte_);
            colorArray[0] = messageArray[0];
            colorArray[1] = messageArray[1];
            byte newR = Bit2Byte(colorArray);
            colorArray = Byte2Bit(pixelColor.G);
            colorArray[0] = messageArray[2];
            colorArray[1] = messageArray[3];
            colorArray[2] = messageArray[4];
            byte newG = Bit2Byte(colorArray);
            colorArray = Byte2Bit(pixelColor.B);
            colorArray[0] = messageArray[5];
            colorArray[1] = messageArray[6];
            colorArray[2] = messageArray[7];
            byte newB = Bit2Byte(colorArray);
            Color newColor = Color.FromArgb(newR, newG, newB);
            res.SetPixel(i, j, newColor);
            index++;
        }

        public void EjectTextFromImage(Bitmap src, ref BackgroundWorker worker)
        {
            // Считываем из картинки текст длиной, равной размеру текста из исходного // файла, которую мы запомнили в переменной text_size во время записи его
            // содержимого в изображение. В данный размер включены также и длины меток
            // начала и конца сообщения, поскольку они уже присутствовали в исходном
            // тексте.
            //В двойном цикле считываем в подготовленный массив байт отведённого
            //размера (temp_message) символы, закодированные в пикселах изображения.
            //После этого полученный массив байт переводим в строку
            //(temp_str_message), применяя кодировку ASCII, поскольку её достаточно
            //для работы с текстом, состоящим из латинских букв и знаков препинания.
            byte[] temp_message = new byte[text_size];
            int i = 0, j = 0;
            int msg_ind = 0;
            bool stop = false;
            for (; i < src.Width; i++)
            {
                worker.ReportProgress((int)((float)i / src.Width * 33));
                if (worker.CancellationPending)
                    return;
                for (j = 0; j < src.Height; j++)
                {
                    if (msg_ind == text_size)
                        {
                            stop = true;
                            break;
                        }
                        temp_message[msg_ind] = Bit2Byte(DecodeSymbol(ref src,
                        ref i, ref j));
                        msg_ind++;
                }
                if (stop) break;
            }
            string temp_str_message = Encoding.ASCII.GetString(temp_message);
            // Ищем в тексте метку начала. Только если она будет найдена,
            //декодирование будем считать успешным. Поиск будет проводиться на строках
            //(string) для наглядности. Пока не дойдём до конца строки
            //temp_str_message, движемся от её начала и берём из неё подстроку длины
            //метки начала. Сравниваем эту подстроку с меткой начала. Если строки
            //совпадают, отмечаем, что метка начала найдена успешно.
            msg_ind = 0;
            bool start_reading = false;
            while (msg_ind <= temp_str_message.Length)
            {
                worker.ReportProgress((int)((float)msg_ind / temp_str_message.Length
                * 33 + 33));
                if (worker.CancellationPending)
                    return;
                if (label_start == temp_str_message.Substring(msg_ind,
                label_start.Length))
                {
                    msg_ind += label_start.Length;
                    start_reading = true;
                    break;
                }
                else msg_ind++;
            }
            // Если метка начала найдена успешно, ищем метку конца, считывая
            //информацию из изображения. Поиск метки конца сообщения проводится по
            //тому же принципу. Если в итоге метка не была найдена, выводим на экран
            //сообщение об этом, но весь считанный текст всё равно, как и при её
            //успешном поиске, записываем в файл.
            //Прогресс выполнения в данной функции разделён на три части: по
            //количеству отдельных операций. Третья из долей расположена в методе
            //WriteText.
            bool stop_found = false;
            int begin_ind = msg_ind;
            if (start_reading)
            {
                while (msg_ind <= temp_str_message.Length)
                {
                    worker.ReportProgress(Clamp((int)((float)msg_ind
                    / temp_str_message.Length * 33 + 66), 0, 100));
                    if (worker.CancellationPending)
                        return;
                    if (label_end == temp_str_message.Substring(msg_ind,
                    label_end.Length))
                    {
                        stop_found = true;
                        strMessage = temp_str_message.Substring(begin_ind, msg_ind
                        - begin_ind);
                        break;
                    }
                    else msg_ind++;
                    
                }
                if (!stop_found)
                {
                    strMessage = temp_str_message.Substring(begin_ind,
                    temp_str_message.Length - begin_ind);
                    Form1.ShowMsgBox("End label NOT found, text EJECTED!");
                }
            }
            else
            {
                Form1.ShowMsgBox("Text not found!");
                return;
            }
            WriteText();
        }

        private void WriteText()
        {
            byte[] temp_message = Encoding.ASCII.GetBytes(strMessage);
            FileStream SourceStream = File.Open(Form1.ejected_file_name(),
            FileMode.Create);
            SourceStream.Seek(0, SeekOrigin.Begin);
            SourceStream.Write(temp_message, 0, temp_message.Length);
            SourceStream.Close();
        }

        private BitArray DecodeSymbol(ref Bitmap src, ref int i, ref int j)
        {
            Color pixelColor = src.GetPixel(i, j);
            BitArray colorArray = Byte2Bit(pixelColor.R);
            BitArray messageArray = Byte2Bit(pixelColor.R);            
            messageArray[0] = colorArray[0];
            messageArray[1] = colorArray[1];
            colorArray = Byte2Bit(pixelColor.G);
            messageArray[2] = colorArray[0];
            messageArray[3] = colorArray[1];
            messageArray[4] = colorArray[2];
            colorArray = Byte2Bit(pixelColor.B);
            messageArray[5] = colorArray[0];
            messageArray[6] = colorArray[1];
            messageArray[7] = colorArray[2];
            return messageArray;
        }

        public Bitmap EncodeNoisedImage(Bitmap src, ref BackgroundWorker worker)
        {
            return src;
        }

    }
}
