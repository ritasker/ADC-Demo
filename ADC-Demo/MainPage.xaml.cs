using System;
using System.Linq;
using Windows.UI.Xaml.Controls;
using Windows.Devices.Gpio;
using Windows.UI.Xaml;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace ADC_Demo
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        private int _lastRead = 0;
        private int _tolerance = 5;

        private GpioPin _spimosiPin;
        private GpioPin _spimisoPin;
        private GpioPin _spiclkPin;
        private GpioPin _spicsPin;

        private const int SPICLK_PIN = 18;
        private const int SPIMISO_PIN = 23;
        private const int SPIMOSI_PIN = 24;
        private const int SPICS_PIN = 25;

        private const int POTENTIOMETER_ADC = 0;

        public MainPage()
        {
            this.InitializeComponent();

            var timer = new DispatcherTimer();
            timer.Interval = TimeSpan.FromMilliseconds(50);
            timer.Tick += Timer_Tick;

            InitGpio();

            timer.Start();
        }

        private void Timer_Tick(object sender, object e)
        {
            bool potChanged = false;

            int rawValue = ReadAdc(POTENTIOMETER_ADC);

            int potAdjustment = Math.Abs(rawValue - _lastRead);

            if (potAdjustment > _tolerance)
                potChanged = true;

            if (potChanged)
            {
                double potValue = rawValue / 10.24;
                potValue = Math.Round(potValue);
                int finalValue = Convert.ToInt32(potValue);

                potVal_txt.Text = finalValue.ToString();

                _lastRead = finalValue;
            }
        }

        private void InitGpio()
        {
            var gpio = GpioController.GetDefault();

            _spimosiPin = gpio.OpenPin(SPIMOSI_PIN);
            _spimosiPin.SetDriveMode(GpioPinDriveMode.Output);

            _spimisoPin = gpio.OpenPin(SPIMISO_PIN);
            _spimisoPin.SetDriveMode(GpioPinDriveMode.Input);

            _spiclkPin = gpio.OpenPin(SPICLK_PIN);
            _spiclkPin.SetDriveMode(GpioPinDriveMode.Output);

            _spicsPin = gpio.OpenPin(SPICS_PIN);
            _spicsPin.SetDriveMode(GpioPinDriveMode.Output);
        }

        private int ReadAdc(int adcPin)
        {
            if (adcPin > 7 || adcPin < 0)
                throw new ArgumentException("The ADC pin needs to be between 0 - 7.", nameof(adcPin));

            _spicsPin.Write(GpioPinValue.High);
            _spiclkPin.Write(GpioPinValue.Low);
            _spicsPin.Write(GpioPinValue.Low);

            var commandOut = adcPin;
            commandOut |= 0x18;
            commandOut <<= 3;

            foreach (int i in Enumerable.Range(1, 5))
            {
                _spimosiPin.Write((commandOut & 0x80) == 1 ? GpioPinValue.High : GpioPinValue.Low);
                commandOut <<= 1;
                _spiclkPin.Write(GpioPinValue.High);
                _spiclkPin.Write(GpioPinValue.Low);
            }

            int adcOut = 0;

            foreach (int i in Enumerable.Range(1, 12))
            {
                _spiclkPin.Write(GpioPinValue.High);
                _spiclkPin.Write(GpioPinValue.Low);

                adcOut <<= 1;

                if (_spimisoPin.Read() == GpioPinValue.High)
                    adcOut |= 0x1;
            }

            _spicsPin.Write(GpioPinValue.High);

            adcOut >>= 1;
            return adcOut;
        }
    }
}
