using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using KHOpenApi.NET;

namespace StockIndicator
{
    public struct CHART_DATAT
    {
        public double O, H, L, C, V, T;
    }

    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private AxKHOpenAPI axKHOpenAPI;

        public CHART_DATAT[] chart_data;

        public MainWindow()
        {
            InitializeComponent();
            // ActiveX 세팅
            axKHOpenAPI = new AxKHOpenAPI(new WindowInteropHelper(Application.Current.MainWindow).EnsureHandle());
            axKHOpenAPI.OnEventConnect += new _DKHOpenAPIEvents_OnEventConnectEventHandler(this.axKHOpenAPI_OnEventConnect);
            axKHOpenAPI.OnReceiveTrData += new _DKHOpenAPIEvents_OnReceiveTrDataEventHandler(this.axKHOpenAPI_OnReceiveTrData);
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

            chart_data = Array.Empty<CHART_DATAT>();
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
            if (axKHOpenAPI.CommConnect() == 0)
                AddLine("로그인요청중...");
            else
                AddLine("로그인요청 실패");
        }

        private void btn_logout_Click(object sender, RoutedEventArgs e)
        {
            axKHOpenAPI.CommTerminate();
            AddLine("로그아웃");
        }

        private void axKHOpenAPI_OnEventConnect(object sender, _DKHOpenAPIEvents_OnEventConnectEvent e)
        {
            string ss = e.nErrCode.ToString();
            AddLine("<OnEventConnect> : " + ss);
            if (e.nErrCode == 0)
            {
                btn_tr_rreq.IsEnabled = true;
            }
        }

        private void axKHOpenAPI_OnReceiveTrData(object sender, _DKHOpenAPIEvents_OnReceiveTrDataEvent e)
        {
            AddLine(String.Format("<OnReceiveTrData> : {0}, Count={1}"
                , e.sRQName
                , axKHOpenAPI.GetRepeatCnt(e.sTrCode, e.sRQName)
                ));
            if (e.sScrNo == "1004")
            {
                string sItemCode = axKHOpenAPI.GetCommData(e.sTrCode, e.sRQName, 0, "종목코드");
                int nRepeatCount = axKHOpenAPI.GetRepeatCnt(e.sTrCode, e.sRQName);
                bool b분틱 = e.sTrCode == "OPT10079" || e.sTrCode == "OPT10080";
                if (nRepeatCount > 0)
                {
                    chart_data = new CHART_DATAT[nRepeatCount];
                    for (int i = 0; i < nRepeatCount; i++)
                    {
                        ref CHART_DATAT data = ref chart_data[nRepeatCount - i - 1];
                        data.O = Convert.ToDouble(axKHOpenAPI.GetCommData(e.sTrCode, e.sRQName, i, "시가"));
                        data.H = Convert.ToDouble(axKHOpenAPI.GetCommData(e.sTrCode, e.sRQName, i, "고가"));
                        data.L = Convert.ToDouble(axKHOpenAPI.GetCommData(e.sTrCode, e.sRQName, i, "저가"));
                        data.C = Convert.ToDouble(axKHOpenAPI.GetCommData(e.sTrCode, e.sRQName, i, "현재가"));
                        data.V = Convert.ToDouble(axKHOpenAPI.GetCommData(e.sTrCode, e.sRQName, i, "거래량"));
                        if (b분틱)
                            data.T = Convert.ToDouble(axKHOpenAPI.GetCommData(e.sTrCode, e.sRQName, i, "체결시간"));
                        else
                            data.T = Convert.ToDouble(axKHOpenAPI.GetCommData(e.sTrCode, e.sRQName, i, "일자"));
                    }
                    text_DataLength.Text = String.Format("Data Length = {0}", chart_data.Length);
                    for (int i = 0; i < nRepeatCount; i++)
                    {
                        ref CHART_DATAT data = ref chart_data[nRepeatCount - i - 1];
                        AddLine(String.Format("[{0:000}] 타임: {1}, 시가={2}, 고가={3}, 저가={4}, 종가={5}, 거래량={6}"
                            ,i, data.T, data.O, data.H, data.L, data.C, data.V));
                    }
                }
            }
        }

        private void btn_tr_rreq_Click(object sender, RoutedEventArgs e)
        {
            if (axKHOpenAPI.GetConnectState() == 0)
            {
                AddLine("로그인후 조회할수 있습니다");
                return;
            }
            int nRoundType = comdo_round.SelectedIndex;
            string? sInterval = combo_interval.SelectedItem.ToString();
            string sItemCode = text_code.Text;
            string sTRCode = roundTRCodes[nRoundType];
            axKHOpenAPI.SetInputValue("종목코드", sItemCode);
            if (nRoundType < 3)
                axKHOpenAPI.SetInputValue("기준일자", DateTime.Now.ToString("yyyyMMdd"));
            else
                axKHOpenAPI.SetInputValue("틱범위", sInterval);
            //axKHOpenAPI.SetInputValue("끝일자", "");
            axKHOpenAPI.SetInputValue("수정주가구분", "1");
            long ret = axKHOpenAPI.CommRqData(sTRCode, sTRCode, 0, "1004");
            if (ret != 0)
                AddLine("요청실패 : " + ret);
        }

        private void combo_indicator_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.AddedItems.Count > 0)
            {
                string? sIndicator = (string?)e.AddedItems[0];
                if (sIndicator != null) Calculate(sIndicator);
            }
        }

        private void btn_calc_Click(object sender, RoutedEventArgs e)
        {
            Calculate(combo_indicator.Text);
        }

        private double GetParam(string szText, ref int nPos)
        {
            int nNumStartPos = -1;
            for (; nPos < szText.Length; nPos++)
            {
                char c = szText[nPos];
                if (nNumStartPos == -1 && c >= '0' && c <= '9')
                {
                    nNumStartPos = nPos;
                } else if (nNumStartPos != -1 && !((c >= '0' && c <= '9') || c == '.'))
                {
                    string szNum = szText.Substring(nNumStartPos, nPos - nNumStartPos);
                    return Convert.ToDouble(szNum);
                }
            }
            if (nNumStartPos != -1)
            {
                string szNum = szText.Substring(nNumStartPos, nPos - nNumStartPos);
                return Convert.ToDouble(szNum);
            }
            return double.NaN;
        }

        private void Calculate(string szIndiText)
        {
            if (chart_data.Length == 0)
            {
                AddLine($"Chart DataLength = {chart_data.Length}");
            }

            double[] O = CALC.Get_O(chart_data);
            double[] H = CALC.Get_H(chart_data);
            double[] L = CALC.Get_L(chart_data);
            double[] C = CALC.Get_C(chart_data);
            double[] V = CALC.Get_V(chart_data);
            double[] T = CALC.Get_T(chart_data);


            // 파라메터 얻기
            List<double> Params = new List<double>();
            int nPos = 0;
            while (nPos < szIndiText.Length)
            {
                double dParam = GetParam(szIndiText, ref nPos);
                if (dParam == double.NaN) break;
                Params.Add(dParam);
            }

            double Result = double.NaN;
            if (szIndiText.IndexOf("SMA") == 0)
            {
                Result = CALC.avg(C, (int)Params[0]);
            } 
            else if (szIndiText.IndexOf("EMA") == 0)
            {
                Result = CALC.eavg(C, (int)Params[0]);
            }
            else if (szIndiText.IndexOf("MACD") == 0)
            {
                double EmaShort = CALC.eavg(C, (int)Params[0]);
                double EmaLong = CALC.eavg(C, (int)Params[1]);
                Result = EmaShort - EmaLong;
            }
            AddLine($"{szIndiText} = {Result}");
        }

        private string[] roundTypes = { "일", "주", "월", "분", "틱"};
        private string[] roundTRCodes = { "OPT10081", "OPT10082", "OPT10083", "OPT10080", "OPT10079" };
        private string[] intervalTypes = { "1", "3", "5", "10", "15", "20", "30", "60", "120", "300", "600" };
        private string[] Indicators = { "SMA5", "SMA10", "SMA20", "SMA60", "SMA120"
        , "EMA5", "EMA10", "EMA20", "EMA60", "EMA120", "MACD(12,26)"
        //, "RSI14", "CCI9", "BB(20,2)"
        };
    }
}
