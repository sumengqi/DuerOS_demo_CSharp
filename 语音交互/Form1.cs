using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Management; 
using System.Runtime.InteropServices;
using System.Net;
using System.IO;
using System.Media;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NAudio.Wave;
using NAudio.Codecs;
using NAudio.FileFormats;
using NAudio.CoreAudioApi;
using NAudio.Dmo;
using NAudio.Dsp;
using NAudio.Gui;
using NAudio.Midi;
using NAudio.Mixer;
using NAudio.Sfz;
using NAudio.Utils;
using System.Net.NetworkInformation;
using System.Collections;

namespace 语音交互
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        private IWaveIn waveIn;
        private WaveFileWriter writer;
        /// <summary>
        /// 开始录音
        /// </summary>
        private void StartRecording()
        {
            if (waveIn != null) return;
            waveIn = new WaveIn { WaveFormat = new WaveFormat(16000, 1) };//设置码率
            writer = new WaveFileWriter(System.IO.Path.GetTempPath()+"\\rec.wav", waveIn.WaveFormat);
            //writer = new WaveFileWriter("1.wav", waveIn.WaveFormat);
            waveIn.DataAvailable += waveIn_DataAvailable;
            waveIn.RecordingStopped += OnRecordingStopped;
            waveIn.StartRecording();
        }
        /// <summary>
        /// 停止录音
        /// </summary>
        private void StopRecording()
        {
            waveIn.StopRecording();
            waveIn.Dispose();
            writer.Close();
        }
        /// <summary>
        /// 录音中
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void waveIn_DataAvailable(object sender, WaveInEventArgs e)
        {
            if (this.InvokeRequired)
            {
                this.BeginInvoke(new EventHandler<WaveInEventArgs>(waveIn_DataAvailable), sender, e);
            }
            else
            {
                writer.Write(e.Buffer, 0, e.BytesRecorded);
                int secondsRecorded = (int)(writer.Length / writer.WaveFormat.AverageBytesPerSecond);//录音时间获取
                if (secondsRecorded >= 30)
                {
                    StopRecording();
                }

            }
        }
        /// <summary>
        /// 停止录音
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnRecordingStopped(object sender, StoppedEventArgs e)
        {
            if (this.InvokeRequired)
            {
                this.BeginInvoke(new EventHandler<StoppedEventArgs>(OnRecordingStopped), sender, e);
            }
            else
            {
                if (waveIn != null) // 关闭录音对象
                {
                    waveIn.Dispose();
                    waveIn = null;
                }
                if (writer != null)//关闭文件流
                {
                    writer.Close();
                    writer = null;
                }
                if (e.Exception != null)
                {
                    MessageBox.Show(String.Format("出现问题 {0}",
                                                  e.Exception.Message));
                }
            }
        }
            
        public  byte[] HttpPost(string url, byte[] postData)
        {
            WebClient webClient = new WebClient();
            webClient.Headers.Add("authorization", "Bearer 24.686b1ad5963dbf8157444a9b19ab1324.2592000.1503723917.282335-9845303");//token，替换成自己的
            webClient.Headers.Add("dueros-device-id", "879622951");//device-id
            webClient.Headers.Add("content-type", "multipart/form-data; boundary=--879622951");//boundary
            byte[] responseData = webClient.UploadData(url, "POST", postData);//得到返回字符流  
            webClient.Dispose();
            return responseData;
        }
 

        private void button2_MouseDown(object sender, MouseEventArgs e)
        {
            StartRecording();
        }

        private void button2_MouseUp(object sender, MouseEventArgs e)
        {
            axWindowsMediaPlayer1.Ctlcontrols.stop();
            axWindowsMediaPlayer1.currentPlaylist.clear();
            StopRecording();
            string url = "http://dueros-h2.baidu.com/dcs/v1/events";
            string path = "postbody.dat";  //postbody
            FileStream fs = new FileStream(path, FileMode.Open);
            //获取文件大小
            long size = fs.Length;
            byte[] postData = new byte[size];
            //将文件读到byte数组中
            fs.Read(postData, 0, postData.Length);
            fs.Close();
            byte[] end = Encoding.Default.GetBytes("\r\n----879622951--");
            string path1 = System.IO.Path.GetTempPath() + "\\rec.wav";
            FileStream fs1 = new FileStream(path1, FileMode.Open);
            //获取文件大小
            long size1 = fs1.Length;
            byte[] sound = new byte[size1];
            //将文件读到byte数组中
            fs1.Read(sound, 0, sound.Length);
            fs1.Close();
            byte[] resArr = new byte[postData.Length + sound.Length + end.Length];
            postData.CopyTo(resArr, 0);
            sound.CopyTo(resArr, postData.Length);
            end.CopyTo(resArr, postData.Length + sound.Length);
            byte[] http_byte = HttpPost(url, resArr);
            string http = Encoding.Default.GetString(http_byte);
            string[] body = http.Split(new string[1] { "--___dumi_avs_xuejuntao___\r\n" }, StringSplitOptions.None);
            ArrayList al = new ArrayList();
            ArrayList al_j = new ArrayList();
            for (int i = 1; i < body.Length - 1; i++)
            {
                al.Add(body[i].Split(new string[1] { "\r\n\r\n" }, StringSplitOptions.None));
            }
            string[][] http_body = new string[al.Count][];
            al.CopyTo(http_body);
            for (int i = 0; i < http_body.Length; i++)
            {
                if (http_body[i][0].IndexOf("application/json") >= 0)
                {
                    al_j.Add((JObject)JsonConvert.DeserializeObject(http_body[i][1]));
                }
                else if (http_body[i][0].IndexOf("application/octet-stream") >= 0)
                {
                    FileStream fi = new FileStream(System.IO.Path.GetTempPath() + "\\Speak.mp3", FileMode.Create);
                    fi.Write(http_byte, 0, http_byte.Length);
                    fi.Close();
                }
            }
            JObject[] json_body = new JObject[al_j.Count];
            al_j.CopyTo(json_body);
 
            axWindowsMediaPlayer1.currentPlaylist = axWindowsMediaPlayer1.newPlaylist("sound", "");
            for (int i = 0; i < json_body.Length; i++)
            {
                switch (json_body[i]["directive"]["header"]["name"].ToString())
                {
                    case "Speak":
                        //file_put_contents("Speak.mp3",$voice);
                        string speak_url = System.IO.Path.GetTempPath() + "\\Speak.mp3";
                        axWindowsMediaPlayer1.currentPlaylist.appendItem(axWindowsMediaPlayer1.newMedia(speak_url));
                        break;
                    case "Play":
                        string play_url = json_body[i]["directive"]["payload"]["audioItem"]["stream"]["url"].ToString();
                        axWindowsMediaPlayer1.currentPlaylist.appendItem(axWindowsMediaPlayer1.newMedia(play_url));
                        break;
                    case "HtmlView":
                        string view_url = json_body[i]["directive"]["payload"]["url"].ToString();
                        webBrowser1.ScriptErrorsSuppressed = true;
                        webBrowser1.Navigate(view_url);
                        break;
                    default:
                        break;
                }
            }
            axWindowsMediaPlayer1.Ctlcontrols.play();
        }
    }
}
