namespace StockIndicator
{
    internal class CALC
    {
        public static double[] Get_O(CHART_DATAT[] candles)
        {
            double[] value = new double[candles.Length];
            for (int i = 0; i < candles.Length; i++)
            {
                value[i] = candles[i].O;
            }
            return value;
        }
        public static double[] Get_H(CHART_DATAT[] candles)
        {
            double[] value = new double[candles.Length];
            for (int i = 0; i < candles.Length; i++)
            {
                value[i] = candles[i].H;
            }
            return value;
        }
        public static double[] Get_L(CHART_DATAT[] candles)
        {
            double[] value = new double[candles.Length];
            for (int i = 0; i < candles.Length; i++)
            {
                value[i] = candles[i].L;
            }
            return value;
        }
        public static double[] Get_C(CHART_DATAT[] candles)
        {
            double[] value = new double[candles.Length];
            for (int i = 0; i < candles.Length; i++)
            {
                value[i] = candles[i].C;
            }
            return value;
        }
        public static double[] Get_V(CHART_DATAT[] candles)
        {
            double[] value = new double[candles.Length];
            for (int i = 0; i < candles.Length; i++)
            {
                value[i] = candles[i].V;
            }
            return value;
        }
        public static double[] Get_T(CHART_DATAT[] candles)
        {
            double[] value = new double[candles.Length];
            for (int i = 0; i < candles.Length; i++)
            {
                value[i] = candles[i].T;
            }
            return value;
        }

        // 합
        public static double sum(double[] src, int Period)
        {
            if (src.Length < Period) return double.NaN;
            double dSum = 0;
            for (int i = src.Length - Period; i < src.Length; i++)
            {
                dSum += src[i];
            }
            return dSum;
        }

        // 단순이평
        public static double avg(double[] src, int Period) => sum(src, Period) / Period;

        // 지수이평
        public static double eavg(double[] src, int Period)
        {
            if (src.Length < Period) return double.NaN;
            double dSum = 0;
            for (int i = 0; i < Period; i++)
            {
                dSum += src[i];
            }
            double dResult = dSum / Period;
            double dK = 2.0 / (Period + 1);
            for (int i = Period; i < src.Length; i++)
            {
                dResult += (src[i] - dResult) * dK;
            }
            return dResult;
        }
    }
}
