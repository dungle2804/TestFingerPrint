using DPUruNet;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Data.SqlClient;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using UareUSampleCSharp;

namespace TestFingerPrint
{
    public partial class frmDBEnrollment : Form
    {
        private int requiredFingerCount = 2; // Change this to specify the required number of fingers.
        private int enrolledFingerCount = 0;  // Keeps track of how many fingers have been enrolled.
        //private Fmd referenceFingerprint;//  store a reference fingerprint during the first enrollment attempt
        public frmDBEnrollment()
        {
            InitializeComponent();
        }
        /// <summary>
        /// Holds fmds enrolled by the enrollment GUI.
        /// </summary>
        public Dictionary<int, Fmd> Fmds
        {
            get { return fmds; }
            set { fmds = value; }
        }
        private Dictionary<int, Fmd> fmds = new Dictionary<int, Fmd>();

        /// <summary>
        /// Reset the UI causing the user to reselect a reader.
        /// </summary>
        public bool Reset
        {
            get { return reset; }
            set { reset = value; }
        }
        private bool reset;


        private enum Action
        {
            UpdateReaderState,
            SendBitmap,
            SendMessage
        }
        private delegate void SendMessageCallback(Action state, object payload);
        private void SendMessage(Action action, object payload)
        {
            try
            {
                if (this.pbFingerprint.InvokeRequired)
                {
                    SendMessageCallback d = new SendMessageCallback(SendMessage);
                    this.Invoke(d, new object[] { action, payload });
                }
                else
                {
                    switch (action)
                    {
                        case Action.SendMessage:
                            MessageBox.Show((string)payload);
                            break;
                        case Action.SendBitmap:
                            pbFingerprint.Image = (Bitmap)payload;
                            pbFingerprint.Refresh();
                            break;
                    }
                }
            }
            catch (Exception)
            {
            }
        }

        private Reader _reader;

        private ReaderSelection _readerSelection;
        /// <summary>
        /// Hookup capture handler and start capture.
        /// </summary>
        /// <param name="OnCaptured">Delegate to hookup as handler of the On_Captured event</param>
        /// <returns>Returns true if successful; false if unsuccessful</returns>
        public bool StartCaptureAsync(Reader.CaptureCallback OnCaptured)
        {
            using (Tracer tracer = new Tracer("Form_Main::StartCaptureAsync"))
            {
                // Activate capture handler
                currentReader.On_Captured += new Reader.CaptureCallback(OnCaptured);

                // Call capture
                if (!CaptureFingerAsync())
                {
                    return false;
                }

                return true;
            }
        }
        /// <summary>
        /// Check the device status before starting capture.
        /// </summary>
        /// <returns></returns>
        public void GetStatus()
        {
            using (Tracer tracer = new Tracer("Form_Main::GetStatus"))
            {
                Constants.ResultCode result = currentReader.GetStatus();

                if ((result != Constants.ResultCode.DP_SUCCESS))
                {
                    reset = true;
                    throw new Exception("" + result);
                }

                if ((currentReader.Status.Status == Constants.ReaderStatuses.DP_STATUS_BUSY))
                {
                    Thread.Sleep(50);
                }
                else if ((currentReader.Status.Status == Constants.ReaderStatuses.DP_STATUS_NEED_CALIBRATION))
                {
                    currentReader.Calibrate();
                }
                else if ((currentReader.Status.Status != Constants.ReaderStatuses.DP_STATUS_READY))
                {
                    throw new Exception("Reader Status - " + currentReader.Status.Status);
                }
            }
        }
        /// <summary>
        /// Function to capture a finger. Always get status first and calibrate or wait if necessary.  Always check status and capture errors.
        /// </summary>
        /// <param name="fid"></param>
        /// <returns></returns>
        public bool CaptureFingerAsync()
        {
            using (Tracer tracer = new Tracer("Form_Main::CaptureFingerAsync"))
            {
                try
                {
                    GetStatus();

                    Constants.ResultCode captureResult = currentReader.CaptureAsync(Constants.Formats.Fid.ANSI, Constants.CaptureProcessing.DP_IMG_PROC_DEFAULT, currentReader.Capabilities.Resolutions[0]);
                    if (captureResult != Constants.ResultCode.DP_SUCCESS)
                    {
                        reset = true;
                        throw new Exception("" + captureResult);
                    }

                    return true;
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Error:  " + ex.Message);
                    return false;
                }
            }
        }
        /// <summary>
        /// Cancel the capture and then close the reader.
        /// </summary>
        /// <param name="OnCaptured">Delegate to unhook as handler of the On_Captured event </param>
        public void CancelCaptureAndCloseReader(Reader.CaptureCallback OnCaptured)
        {
            using (Tracer tracer = new Tracer("Form_Main::CancelCaptureAndCloseReader"))
            {
                if (currentReader != null)
                {
                    currentReader.CancelCapture();

                    // Dispose of reader handle and unhook reader events.
                    currentReader.Dispose();

                    if (reset)
                    {
                        CurrentReader = null;
                    }
                }
            }
        }
        // When set by child forms, shows s/n and enables buttons.
        private Reader currentReader;
        public Reader CurrentReader
        {
            get { return currentReader; }
            set
            {
                currentReader = value;
                SendMessage(Action.UpdateReaderState, value);
            }
        }
        private ReaderCollection _readers;
        private void LoadScanners()
        {
            cboReaders.Text = string.Empty;
            cboReaders.Items.Clear();
            cboReaders.SelectedIndex = -1;

            try
            {
                _readers = ReaderCollection.GetReaders();

                foreach (Reader Reader in _readers)
                {
                    cboReaders.Items.Add(Reader.Description.Name);
                }

                if (cboReaders.Items.Count > 0)
                {
                    cboReaders.SelectedIndex = 0;
                    //btnCaps.Enabled = true;
                    //btnSelect.Enabled = true;
                }
                else
                {
                    //btnSelect.Enabled = false;
                    //btnCaps.Enabled = false;
                }
            }
            catch (Exception ex)
            {
                //message box:
                String text = ex.Message;
                text += "\r\n\r\nPlease check if DigitalPersona service has been started";
                String caption = "Cannot access readers";
                MessageBox.Show(text, caption);
            }
        }
        private void frmDBEnrollment_Load(object sender, EventArgs e)
        {
            // Reset variables
            LoadScanners();
            firstFinger = null;
            resultEnrollment = null;
            preenrollmentFmds = new List<Fmd>();
            pbFingerprint.Image = null;
            if (CurrentReader != null)
            {
                CurrentReader.Dispose();
                CurrentReader = null;
            }
            CurrentReader = _readers[cboReaders.SelectedIndex];
            if (!OpenReader())
            {
                //this.Close();
            }

            if (!StartCaptureAsync(this.OnCaptured))
            {
                //this.Close();
            }



        }
        /// <summary>
        /// Open a device and check result for errors.
        /// </summary>
        /// <returns>Returns true if successful; false if unsuccessful</returns>
        public bool OpenReader()
        {
            using (Tracer tracer = new Tracer("Form_Main::OpenReader"))
            {
                reset = false;
                Constants.ResultCode result = Constants.ResultCode.DP_DEVICE_FAILURE;

                // Open reader
                result = currentReader.Open(Constants.CapturePriority.DP_PRIORITY_COOPERATIVE);

                if (result != Constants.ResultCode.DP_SUCCESS)
                {
                    MessageBox.Show("Error:  " + result);
                    reset = true;
                    return false;
                }

                return true;
            }
        }
        /// <summary>
        /// Check quality of the resulting capture.
        /// </summary>
        public bool CheckCaptureResult(CaptureResult captureResult)
        {
            using (Tracer tracer = new Tracer("Form_Main::CheckCaptureResult"))
            {
                if (captureResult.Data == null || captureResult.ResultCode != Constants.ResultCode.DP_SUCCESS)
                {
                    if (captureResult.ResultCode != Constants.ResultCode.DP_SUCCESS)
                    {
                        reset = true;
                        throw new Exception(captureResult.ResultCode.ToString());
                    }

                    // Send message if quality shows fake finger
                    if ((captureResult.Quality != Constants.CaptureQuality.DP_QUALITY_CANCELED))
                    {
                        throw new Exception("Quality - " + captureResult.Quality);
                    }
                    return false;
                }

                return true;
            }
        }
        private const int PROBABILITY_ONE = 0x7fffffff;
        private Fmd firstFinger;
        int count = 0;
        DataResult<Fmd> resultEnrollment;
        List<Fmd> preenrollmentFmds;
        /// <summary>
        /// Handler for when a fingerprint is captured.
        /// </summary>
        /// <param name="captureResult">contains info and data on the fingerprint capture</param>

        //public void OnCaptured(CaptureResult captureResult)
        //{
        //    try
        //    {
        //        //Check for 
        //        // Check capture quality and throw an error if bad.
        //        if (!CheckCaptureResult(captureResult)) return;

        //        // Create bitmap
        //        foreach (Fid.Fiv fiv in captureResult.Data.Views)
        //        {
        //            SendMessage(Action.SendBitmap, CreateBitmap(fiv.RawImage, fiv.Width, fiv.Height));
        //        }

        //        //Enrollment Code:
        //        try
        //        {
        //            count++;
        //            // Check capture quality and throw an error if bad.
        //            DataResult<Fmd> resultConversion = FeatureExtraction.CreateFmdFromFid(captureResult.Data, Constants.Formats.Fmd.ANSI);

        //            MessageBox.Show("A finger was captured.  \r\nCount:  " + (count));

        //            if (resultConversion.ResultCode != Constants.ResultCode.DP_SUCCESS)
        //            {
        //                Reset = true;
        //                throw new Exception(resultConversion.ResultCode.ToString());
        //            }

        //            preenrollmentFmds.Add(resultConversion.Data);

        //            if (count == 4)
        //            {
        //                resultEnrollment = DPUruNet.Enrollment.CreateEnrollmentFmd(Constants.Formats.Fmd.ANSI, preenrollmentFmds);

        //                if (resultEnrollment.ResultCode == Constants.ResultCode.DP_SUCCESS)
        //                {
        //                    preenrollmentFmds.Clear();
        //                    count = 0;
        //                    //obj_bal_ForAll.BAL_StoreCustomerFPData("tbl_Finger", txtledgerId.Text, Fmd.SerializeXml(resultEnrollment.Data));
        //                    MessageBox.Show("Customer Finger Print was successfully enrolled.");
        //                    return;
        //                }
        //                else if (resultEnrollment.ResultCode == Constants.ResultCode.DP_ENROLLMENT_INVALID_SET)
        //                {
        //                    SendMessage(Action.SendMessage, "Enrollment was unsuccessful.  Please try again.");
        //                    preenrollmentFmds.Clear();
        //                    count = 0;
        //                    return;
        //                }
        //            }
        //            else if (count > 4) // Tu so sanh van tay giua cac lan chay, neu vuot 4 lan se loi
        //            {
        //                MessageBox.Show("Invalid Fingerprint, try again");
        //                count = 0;
        //            }
        //            MessageBox.Show("Now place the same finger on the reader.");
        //        }
        //        catch (Exception ex)
        //        {
        //            // Send error message, then close form
        //            SendMessage(Action.SendMessage, "Error:  " + ex.Message);
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        // Send error message, then close form
        //        SendMessage(Action.SendMessage, "Error:  " + ex.Message);
        //    }
        //}

        public void OnCaptured(CaptureResult captureResult)
        {
            try
            {
                // Check capture quality and throw an error if bad.
                if (!CheckCaptureResult(captureResult)) return;

                // Create bitmap
                foreach (Fid.Fiv fiv in captureResult.Data.Views)
                {
                    SendMessage(Action.SendBitmap, CreateBitmap(fiv.RawImage, fiv.Width, fiv.Height));
                }

                // Verification Code:
                try
                {
                    count++;

                    // Check capture quality and throw an error if bad.
                    DataResult<Fmd> resultConversion = FeatureExtraction.CreateFmdFromFid(captureResult.Data, Constants.Formats.Fmd.ANSI);

                    MessageBox.Show("A finger was captured.  \r\nCount:  " + (count));

                    if (resultConversion.ResultCode != Constants.ResultCode.DP_SUCCESS)
                    {
                        Reset = true;
                        throw new Exception(resultConversion.ResultCode.ToString());
                    }

                    // Retrieve stored fingerprints from the database
                    List<Fmd> storedFmds = RetrieveFingerprintsFromDatabase();
                    bool fingerprintMatched = false;

                    //List<Fmd> storedIdenticalTry = null; //storedIddenticalTry.add()
                    //bool identicalFingerTry = true;
                    //if (count > 1) // Compare with the first attempt
                    //{
                    //    CompareResult compare = Comparison.Compare(resultConversion.Data, 0, preenrollmentFmds[0], 0);
                    //    if (compare.ResultCode != Constants.ResultCode.DP_SUCCESS || Convert.ToDouble(compare.Score.ToString()) != 0)
                    //    {
                    //        identicalFingerTry = false;
                    //    }
                    //    else identicalFingerTry = true;
                    //}


                    foreach (Fmd existingFmd in storedFmds)
                    {
                        // Compare the captured fingerprint with each existing fingerprint.
                        CompareResult compare = Comparison.Compare(resultConversion.Data, 0, existingFmd, 0);

                        if (compare.ResultCode != Constants.ResultCode.DP_SUCCESS)
                        {
                            Reset = true;
                            throw new Exception(compare.ResultCode.ToString());
                        }

                        if (Convert.ToDouble(compare.Score.ToString()) == 0)
                        {
                            fingerprintMatched = true;
                            MessageBox.Show("Fingerprint already registered. Please choose another finger.");
                            count = 0;
                            //break;  //Exit the loop when a match is found.
                        }
                    }

                    if (!fingerprintMatched)
                    {

                        preenrollmentFmds.Add(resultConversion.Data);

                        if (count == 4)
                        {
                            resultEnrollment = DPUruNet.Enrollment.CreateEnrollmentFmd(Constants.Formats.Fmd.ANSI, preenrollmentFmds);

                            if (resultEnrollment.ResultCode == Constants.ResultCode.DP_SUCCESS)
                            {
                                preenrollmentFmds.Clear();
                                enrolledFingerCount++;
                                count = 0;

                                if (enrolledFingerCount == requiredFingerCount) // Check if users match the required fingerprints
                                {
                                    MessageBox.Show("Customer Fingerprints were successfully enrolled.");
                                    btnSave.Enabled = false; // Disable the "Save" button
                                }
                                else
                                {
                                    MessageBox.Show(enrolledFingerCount + " Fingerprint enrolled. Please place another finger.");
                                }

                                return;
                            }
                            else if (resultEnrollment.ResultCode == Constants.ResultCode.DP_ENROLLMENT_INVALID_SET)
                            {
                                SendMessage(Action.SendMessage, "Enrollment was unsuccessful. Please try again.");
                                preenrollmentFmds.Clear();
                                count = 0;
                                return;
                            }
                        }
                        else if (count > 4)
                        {
                            MessageBox.Show("Invalid Fingerprint! Please try Again");
                            count = 0;
                        }
                        //else if (!identicalFingerTry)
                        //    {
                        //        MessageBox.Show("Not Identical between each try");
                        //        count--;
                        //    }
                            MessageBox.Show("Now place the same finger on the reader.");
                    }
                }
                catch (Exception ex)
                {
                    // Send error message, then close the form
                    SendMessage(Action.SendMessage, "Error: " + ex.Message);
                }
            }
            catch (Exception ex)
            {
                // Send error message, then close the form
                SendMessage(Action.SendMessage, "Error: " + ex.Message);
            }
        }
        private List<Fmd> RetrieveFingerprintsFromDatabase()
        {
            List<Fmd> storedFmds = new List<Fmd>();
            try
            {
                conn.Close();
                conn.Open();
                SqlDataAdapter cmd = new SqlDataAdapter("Select * from tblFinger", conn);
                DataTable dt = new DataTable();
                cmd.Fill(dt);
                conn.Close();

                for (int i = 0; i < dt.Rows.Count; i++)
                {
                    Fmd val = Fmd.DeserializeXml(dt.Rows[i]["CustomerFinger"].ToString());
                    storedFmds.Add(val);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
            return storedFmds;
        }

        /// <summary>
        /// Create a bitmap from raw data in row/column format.
        /// </summary>
        /// <param name="bytes"></param>
        /// <param name="width"></param>
        /// <param name="height"></param>
        /// <returns></returns>
        public Bitmap CreateBitmap(byte[] bytes, int width, int height)
        {
            byte[] rgbBytes = new byte[bytes.Length * 3];

            for (int i = 0; i <= bytes.Length - 1; i++)
            {
                rgbBytes[(i * 3)] = bytes[i];
                rgbBytes[(i * 3) + 1] = bytes[i];
                rgbBytes[(i * 3) + 2] = bytes[i];
            }
            Bitmap bmp = new Bitmap(width, height, PixelFormat.Format24bppRgb);

            BitmapData data = bmp.LockBits(new Rectangle(0, 0, bmp.Width, bmp.Height), ImageLockMode.WriteOnly, PixelFormat.Format24bppRgb);

            for (int i = 0; i <= bmp.Height - 1; i++)
            {
                IntPtr p = new IntPtr(data.Scan0.ToInt64() + data.Stride * i);
                System.Runtime.InteropServices.Marshal.Copy(rgbBytes, i * bmp.Width * 3, p, bmp.Width * 3);
            }

            bmp.UnlockBits(data);

            return bmp;
        }


        public SqlConnection conn = new SqlConnection("Data Source=HUGO-PC\\SQLEXPRESS01;Initial Catalog=fingerPrintEnrollment;Integrated Security=True;MultipleActiveResultSets=True;");

        private void frmDBEnrollment_FormClosing(object sender, FormClosingEventArgs e)
        {
            CancelCaptureAndCloseReader(this.OnCaptured);
        }



        //private void btnSave_Click(object sender, EventArgs e)
        //{
        //    if (resultEnrollment != null)
        //    {
        //        try
        //        {
        //            conn.Close();
        //            conn.Open();
        //            SqlCommand cmd = new SqlCommand("INSERT INTO tblFinger (LedgerId, CustomerFinger, Name) VALUES (@LedgerId, @CustomerFinger, @Name)", conn);

        //            // Use parameterized queries to avoid SQL injection
        //            cmd.Parameters.AddWithValue("@LedgerId", txtLedgerId.Text);
        //            cmd.Parameters.AddWithValue("@CustomerFinger", Fmd.SerializeXml(resultEnrollment.Data));
        //            cmd.Parameters.AddWithValue("@Name", txtName.Text);

        //            cmd.ExecuteNonQuery();
        //            conn.Close();
        //        }
        //        catch (Exception ex)
        //        {
        //            MessageBox.Show(ex.Message);
        //        }
        //    }
        //}
        private void btnSave_Click(object sender, EventArgs e)
        {
            if (resultEnrollment != null)
            {
                try
                {
                    conn.Close();
                    conn.Open();

                    // Check the number of existing records with the same ID
                    SqlCommand checkCmd = new SqlCommand("SELECT COUNT(*) FROM tblFinger WHERE LedgerId = @LedgerId", conn);
                    checkCmd.Parameters.AddWithValue("@LedgerId", txtLedgerId.Text);

                    int existingRecordCount = (int)checkCmd.ExecuteScalar();

                    if (existingRecordCount >= 2)
                    {
                        MessageBox.Show("Two entries with the same ID already exist. You cannot add another entry with the same ID.");
                    }
                    else
                    {
                        // If there are fewer than 2 records with the same ID, proceed with insertion
                        SqlCommand cmd = new SqlCommand("INSERT INTO tblFinger (LedgerId, CustomerFinger, Name) VALUES (@LedgerId, @CustomerFinger, @Name)", conn);

                        // Use parameterized queries to avoid SQL injection
                        cmd.Parameters.AddWithValue("@LedgerId", txtLedgerId.Text);
                        cmd.Parameters.AddWithValue("@CustomerFinger", Fmd.SerializeXml(resultEnrollment.Data));
                        cmd.Parameters.AddWithValue("@Name", txtName.Text);

                        cmd.ExecuteNonQuery();
                        MessageBox.Show("Fingerprint Save Successfully");
                    }

                    conn.Close();
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message);
                }
            }
        }



        private void label2_Click(object sender, EventArgs e)
        {

        }
    }
}
