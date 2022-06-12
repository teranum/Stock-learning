using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace StockIndicator
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private AxKHOpenAPI64Lib.AxKHOpenAPI64 axKHOpenAPI64;
        public MainWindow()
        {
            InitializeComponent();
            // ActiveX 세팅
            axKHOpenAPI64 = new AxKHOpenAPI64Lib.AxKHOpenAPI64();
            axKHOpenAPI64.OnEventConnect += new AxKHOpenAPI64Lib._DKHOpenAPIEvents_OnEventConnectEventHandler(this.axKHOpenAPI641_OnEventConnect);
            axKHOpenAPI64.OnReceiveTrData += new AxKHOpenAPI64Lib._DKHOpenAPIEvents_OnReceiveTrDataEventHandler(this.axKHOpenAPI641_OnReceiveTrData);
            axContainer.Child = axKHOpenAPI64;
            // 콤보박스 세팅
            for (int i = 0; i < roundTypes.Length; i++)
                comdo_round.Items.Add(roundTypes[i]);
            comdo_round.SelectedIndex = 0;
            for (int i = 0; i < intervalTypes.Length; i++)
                combo_interval.Items.Add(intervalTypes[i]);
            combo_interval.SelectedIndex = 0;
            for (int i = 0; i < Indicators.Length; i++)
                combo_indicator.Items.Add(Indicators[i]);
            AddLine("start");
        }

        private void AddLine(string text)
        {
            //if (listBox_Log.Items.Count > 100) listBox_Log.Items.Clear();
            string sTime = DateTime.Now.ToString("HH:mm:ss.fff : ");
            sTime += text;
            listBox_Log.Items.Add(sTime);
        }

        private void btn_clear_Click(object sender, RoutedEventArgs e)
        {
            listBox_Log.Items.Clear();
        }

        private void btn_login_Click(object sender, RoutedEventArgs e)
        {
            if (axKHOpenAPI64.CommConnect() == 0)
                AddLine("로그인요청중...");
            else
                AddLine("로그인요청 실패");
        }

        private void btn_logout_Click(object sender, RoutedEventArgs e)
        {
            axKHOpenAPI64.CommTerminate();
            AddLine("로그아웃");
        }

        private void axKHOpenAPI641_OnEventConnect(object sender, AxKHOpenAPI64Lib._DKHOpenAPIEvents_OnEventConnectEvent e)
        {
            string ss = e.nErrCode.ToString();
            AddLine("<OnEventConnect> : " + ss);
            if (e.nErrCode == 0)
            {
                btn_tr_rreq.IsEnabled = true;
            }
        }

        public struct CHART_DATAT
        {
            public int O, H, L, C, V;
            public Int64 T;
        }
        public CHART_DATAT[] chart_data;

        private void axKHOpenAPI641_OnReceiveTrData(object sender, AxKHOpenAPI64Lib._DKHOpenAPIEvents_OnReceiveTrDataEvent e)
        {
            AddLine(String.Format("<OnReceiveTrData> : {0}, Count={1}"
                , e.sRQName
                , axKHOpenAPI64.GetRepeatCnt(e.sTrCode, e.sRQName)
                ));
            if (e.sScrNo == "1004")
            {
                string sItemCode = axKHOpenAPI64.GetCommData(e.sTrCode, e.sRQName, 0, "종목코드");
                int nRepeatCount = axKHOpenAPI64.GetRepeatCnt(e.sTrCode, e.sRQName);
                bool b분틱 = e.sTrCode == "OPT10079" || e.sTrCode == "OPT10080";
                if (nRepeatCount > 0)
                {
                    chart_data = new CHART_DATAT[nRepeatCount];
                    for (int i = 0; i < nRepeatCount; i++)
                    {
                        ref CHART_DATAT data = ref chart_data[i];
                        data.O = Convert.ToInt32(axKHOpenAPI64.GetCommData(e.sTrCode, e.sRQName, i, "시가"));
                        data.H = Convert.ToInt32(axKHOpenAPI64.GetCommData(e.sTrCode, e.sRQName, i, "고가"));
                        data.L = Convert.ToInt32(axKHOpenAPI64.GetCommData(e.sTrCode, e.sRQName, i, "저가"));
                        data.C = Convert.ToInt32(axKHOpenAPI64.GetCommData(e.sTrCode, e.sRQName, i, "현재가"));
                        data.V = Convert.ToInt32(axKHOpenAPI64.GetCommData(e.sTrCode, e.sRQName, i, "거래량"));
                        if (b분틱)
                            data.T = Convert.ToInt64(axKHOpenAPI64.GetCommData(e.sTrCode, e.sRQName, i, "체결시간"));
                        else
                            data.T = Convert.ToInt64(axKHOpenAPI64.GetCommData(e.sTrCode, e.sRQName, i, "일자"));
                    }
                    text_DataLength.Text = String.Format("Data Length = {0}", chart_data.Length);
                    for (int i = 0; i < nRepeatCount; i++)
                    {
                        ref CHART_DATAT data = ref chart_data[i];
                        AddLine(String.Format("[{0:000}] 타임: {1}, 시가={2}, 고가={3}, 저가={4}, 종가={5}, 거래량={6}"
                            ,i, data.T, data.O, data.H, data.L, data.C, data.V));
                    }
                }
            }
        }

        private void btn_tr_rreq_Click(object sender, RoutedEventArgs e)
        {
            if (axKHOpenAPI64.GetConnectState() == 0)
            {
                AddLine("로그인후 조회할수 있습니다");
                return;
            }
            int nRoundType = comdo_round.SelectedIndex;
            string? sInterval = combo_interval.SelectedItem.ToString();
            string sItemCode = text_code.Text;
            string sTRCode = roundTRCodes[nRoundType];
            axKHOpenAPI64.SetInputValue("종목코드", sItemCode);
            if (nRoundType < 3)
                axKHOpenAPI64.SetInputValue("기준일자", DateTime.Now.ToString("yyyyMMdd"));
            else
                axKHOpenAPI64.SetInputValue("틱범위", sInterval);
            //axKHOpenAPI64.SetInputValue("끝일자", "");
            axKHOpenAPI64.SetInputValue("수정주가구분", "1");
            long ret = axKHOpenAPI64.CommRqData(sTRCode, sTRCode, 0, "1004");
            if (ret != 0)
                AddLine("요청실패 : " + ret);
        }

        private void combo_indicator_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            AddLine("Indicator Changed : " + e.AddedItems[0]);
        }

        private void btn_calc_Click(object sender, RoutedEventArgs e)
        {

        }

        private string[] roundTypes = { "일", "주", "월", "분", "틱"};
        private string[] roundTRCodes = { "OPT10081", "OPT10082", "OPT10083", "OPT10080", "OPT10079" };
        private string[] intervalTypes = { "1", "3", "5", "10", "15", "20", "30", "60", "120", "300", "600" };
        private string[] Indicators = { "SMA5", "SMA10", "SMA20", "SMA60", "SMA120", "EMA5"
        , "EMA10", "EMA20", "EMA60", "EMA120", "MACD", "RSI", "CCI"};
    }
}
