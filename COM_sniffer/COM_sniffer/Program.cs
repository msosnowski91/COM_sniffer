using System;
using System.IO.Ports;
using System.Text;
using System.Threading;
using System.Diagnostics;

namespace COM_sniffer
{
    class Program
    {
        static SerialPort inputSerialPort = new SerialPort();
        static SerialPort middleSerialPort = new SerialPort();
        static SerialPort outputSerialPort = new SerialPort();
        static string servicePortAddress = "1";
        static bool _continue = true;
        static int performanceTime = 25;

        static void Main(string[] args)
        {
            Initialize(args);
            inputSerialPort.Open();
            middleSerialPort.Open();

            string deviceRead;
            string deviceWrite;
            int counter;
            byte[] ba;
            bool modbusMode = true;
            while (_continue)
            {
                if (Console.KeyAvailable)
                {
                    if (Console.ReadKey(true).Key == ConsoleKey.F4) Console.WriteLine("F4");
                    {
                        string consoleInput = Console.ReadLine();
                        switch (consoleInput)
                        {
                            case "quit":
                                _continue = false;
                                break;
                            case "clear":
                                Console.Clear();
                                Console.ResetColor();
                                break;
                            case "modbus on":
                                modbusMode = true;
                                Console.WriteLine("modbus on!");
                                break;
                            case "modbus off":
                                modbusMode = false;
                                Console.WriteLine("modbus off!");

                                break;
                            default:
                                break;
                        }
                    }
                }

                counter = inputSerialPort.BytesToRead;
                Thread.Sleep(performanceTime);

                if (inputSerialPort.BytesToRead > 0 && (counter == inputSerialPort.BytesToRead))
                {
                    ba = GetReadBufferContent(inputSerialPort);
                    deviceRead = ByteArrayToString(ba);
                    Console.BackgroundColor = ConsoleColor.Blue;
                    Console.WriteLine($"R: {deviceRead}");
                    middleSerialPort.Write(ba, 0, ba.Length);
                    if (modbusMode)
                    {
                        string modbusInfo = decryptReadModbusInfo(ba);
                        if (modbusInfo.Length > 3) Console.WriteLine(modbusInfo);
                    }
                    Console.ResetColor();
                }

                counter = middleSerialPort.BytesToRead;
                Thread.Sleep(performanceTime);

                if (middleSerialPort.BytesToRead > 0 && (counter == middleSerialPort.BytesToRead))
                {
                    ba = GetReadBufferContent(middleSerialPort);
                    deviceWrite = ByteArrayToString(ba);
                    Console.BackgroundColor = ConsoleColor.Red;
                    Console.WriteLine($"W: {deviceWrite}");
                    inputSerialPort.Write(ba, 0, ba.Length);
                    if (modbusMode)
                    {
                        string modbusInfo = decryptWrittenModbusInfo(ba);
                        if(modbusInfo.Length>3)Console.WriteLine(modbusInfo);
                    }
                    Console.ResetColor();
                }

                Thread.Sleep(performanceTime);
            }
            Console.WriteLine("Press any key to exit...");
            Console.ReadKey();
        }

        private static string decryptReadModbusInfo(byte[] ba)
        {
            string output = "";
            string slaveAddress;
            string functionCode;
            string startAddress;
            int byteCount;
            int numberOfRegisters;
            string[] data;
            int length = ba.Length;
            if (length > 0) slaveAddress = ba[0].ToString("000");
            else return output;
            if (length > 1) functionCode = ba[1].ToString("00");
            else return output;
            switch (functionCode)
            {
                case "03":
                    if (length > 2)
                    {
                        byteCount = ba[2];
                        numberOfRegisters = byteCount / 4;
                    }
                    else return output;
                    if(length==(3+byteCount+2))
                    {
                        output = $"SlaveAdd: {slaveAddress} Func: {functionCode} NumberOfRegisters: {numberOfRegisters.ToString("00")} ";
                        data = new string[numberOfRegisters];
                        for (int i = 0; i < numberOfRegisters; i++)
                        {
                            data[i] = BitConverter.ToSingle(new byte[] { ba[2 + i * 4 + 1], ba[2 + i * 4 + 2], ba[2 + i * 4 + 3], ba[2 + i * 4 + 4] }, 0).ToString();
                            output += $"Value{i.ToString()}: {data[i]} ";
                        }
                    }
                    else return output;
                    break;
                case "16":
                    if (length > 3) startAddress = (BitConverter.ToInt16(new byte[] { ba[3], ba[2] }, 0).ToString("00000"));
                    else return output;
                    if (length > 5) numberOfRegisters = (BitConverter.ToInt16(new byte[] { ba[5], ba[4] }, 0));
                    else return output;
                    output = $"SlaveAdd: {slaveAddress} Func: {functionCode} StartAdd: {startAddress} NumberOfRegisters: {numberOfRegisters.ToString("00")} ";
                    break;
                default:
                    break;
            }
            return output;
        }

        private static string decryptWrittenModbusInfo(byte[] ba)
        {
            string output = "";
            string slaveAddress;
            string functionCode;
            string startAddress;
            int byteCount;
            int numberOfRegisters;
            string[] data;
            int length = ba.Length;
            if (length > 0) slaveAddress = ba[0].ToString("000");
            else return output;
            if (length > 1) functionCode = ba[1].ToString("00");
            else return output;
            if (length > 3) startAddress = (BitConverter.ToInt16(new byte[] { ba[3], ba[2] }, 0).ToString("00000"));
            else return output;
            if (length > 5) numberOfRegisters = (BitConverter.ToInt16(new byte[] { ba[5], ba[4] }, 0));
            else return output;

            switch (functionCode)
            {
                case "03":
                   output = $"SlaveAdd: {slaveAddress} Func: {functionCode} StartAdd: {startAddress} NumberOfRegisters: {numberOfRegisters.ToString("00")}";
                    break;
                case "16":
                    if (length > 6)
                    {
                        byteCount = ba[6];
                        numberOfRegisters = byteCount / 4;
                    }
                    else return output;
                    if (length == (7 + byteCount + 2))
                    {
                        output = $"SlaveAdd: {slaveAddress} Func: {functionCode} StartAdd: {startAddress} NumberOfRegisters: {numberOfRegisters.ToString("00")} ";
                        data = new string[numberOfRegisters];
                        for (int i = 0; i < numberOfRegisters; i++)
                        {
                            var sfdadsf = new byte[] { ba[6 + i * 4 + 1], ba[6 + i * 4 + 2], ba[6 + i * 4 + 3], ba[6 + i * 4 + 4] };
                            data[i] = BitConverter.ToSingle(new byte[] { ba[6 + i * 4 + 1], ba[6 + i * 4 + 2], ba[6 + i * 4 + 3], ba[6 + i * 4 + 4] }, 0).ToString();
                            output += $"Value{i.ToString()}: {data[i]} ";
                        }
                    }
                    break;
                default:
                    break;
            }
            return output;
        }

        private static string ByteArrayToString(byte[] input)
        {
            string output = "";
            int counter = input.Length;
            for (int i = 0; i < counter; i++)
            {
                output += input[i].ToString("X2");
                output += " ";
            }
            return output;
        }

        private static byte[] GetReadBufferContent(SerialPort serialPort)
        {
            int counter = serialPort.BytesToRead;
            byte[] output = new byte[counter];
            for (int i = 0; i < counter; i++)
            {
                output[i] = Convert.ToByte(serialPort.ReadByte());
            }
            return output;
        }

        public static void Initialize(string[] args)
        {
            Console.ResetColor();
            int length = args.Length;
            if (length >= 1) inputSerialPort.PortName = args[0];
            else inputSerialPort.PortName = "COM1";
            if (length >= 2) servicePortAddress = args[1];
            inputSerialPort.BaudRate = 38400;
            inputSerialPort.DataBits = 8;
            inputSerialPort.StopBits = StopBits.Two;
            inputSerialPort.Parity = Parity.None;
            inputSerialPort.Handshake = Handshake.None;

            inputSerialPort.ReadTimeout = 500;
            inputSerialPort.WriteTimeout = 500;

            if (length >= 3) outputSerialPort.PortName = args[2];
            else outputSerialPort.PortName = "COM2";
            outputSerialPort.BaudRate = 38400;
            outputSerialPort.DataBits = 8;
            outputSerialPort.StopBits = StopBits.Two;
            outputSerialPort.Parity = Parity.None;
            outputSerialPort.Handshake = Handshake.None;

            if (length >= 4) middleSerialPort.PortName = args[3];
            else middleSerialPort.PortName = "COM3";
            middleSerialPort.BaudRate = 38400;
            middleSerialPort.DataBits = 8;
            middleSerialPort.StopBits = StopBits.Two;
            middleSerialPort.Parity = Parity.None;
            middleSerialPort.Handshake = Handshake.None;

            if (length >= 5) performanceTime = int.Parse(args[4]);
            else performanceTime = 25;

            middleSerialPort.ReadTimeout = 500;
            middleSerialPort.WriteTimeout = 500;
        }


    }
}
