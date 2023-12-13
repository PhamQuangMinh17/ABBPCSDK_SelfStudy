using ABB.Robotics.Controllers;
using ABB.Robotics.Controllers.Discovery;
using ABB.Robotics.Controllers.RapidDomain;

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;


namespace PCSDK_Exp_1
{
    public partial class Form1 : Form
    {
        // Local Variables 
        private Controller controller = null;
        private ABB.Robotics.Controllers.RapidDomain.Task[] tasks = null;
        private NetworkScanner scanner = null;
        private NetworkWatcher networkwatcher = null;

        public Form1()
        {
            InitializeComponent();
            // Initializing network scanner and watcher
            NetworkScanning();
            SetupNetworkWatcher();
        }

        /* Scan the network as soon as the application start */
        private void NetworkScanning()
        {
            /* Scan Network for Controllers*/
            this.scanner = new NetworkScanner();
            this.scanner.Scan();
            ControllerInfoCollection controllers = scanner.Controllers;

            /* Add discovered Controllers t ListView */
            ListViewItem item = null; // Create empty listview
            foreach (ControllerInfo controllerInfo in controllers)
            {
                item = new ListViewItem(controllerInfo.IPAddress.ToString());
                item.SubItems.Add(controllerInfo.Id);
                item.SubItems.Add(controllerInfo.Availability.ToString());
                item.SubItems.Add(controllerInfo.IsVirtual.ToString());
                item.SubItems.Add(controllerInfo.SystemName);
                item.SubItems.Add(controllerInfo.Version.ToString());
                item.SubItems.Add(controllerInfo.ControllerName);

                listView1.Items.Add(item);
                item.Tag = controllerInfo;
            }
        }

        /* Initialize Network Watcher:
         the application can supervise the network and detect when controllers are lost or added.
        */
        private void SetupNetworkWatcher()
        {
            // Initialize NetworkWatcher
            this.networkwatcher = new NetworkWatcher(scanner.Controllers);

            // Subscribe to Found and Lost events
            this.networkwatcher.Found += new EventHandler<NetworkWatcherEventArgs>(HandleFoundEvent);
            this.networkwatcher.Lost += new EventHandler<NetworkWatcherEventArgs>(HandleLostEvent);

            // Enable raising events
            this.networkwatcher.EnableRaisingEvents = true;
        }

        /* Handle event */
        /*  the events will be received on a background thread and should result in an update of the user interface 
         *   --> The Invoke method must be called in the event handler
         *  */
        void HandleFoundEvent(object sender, NetworkWatcherEventArgs e)
        {
            this.Invoke(new EventHandler<NetworkWatcherEventArgs>(AddControllerToListView),
                new Object[] { this, e });
        }

        /*  Event handler AddControllerToListView updates the user interface */
        private void AddControllerToListView(object sender, NetworkWatcherEventArgs e)
        {
            ControllerInfo controllerInfo = e.Controller;
            ListViewItem item = new ListViewItem(controllerInfo.IPAddress.ToString());
            item.SubItems.Add(controllerInfo.Id);
            item.SubItems.Add(controllerInfo.Availability.ToString());
            item.SubItems.Add(controllerInfo.IsVirtual.ToString());
            item.SubItems.Add(controllerInfo.SystemName);
            item.SubItems.Add(controllerInfo.Version.ToString());
            item.SubItems.Add(controllerInfo.ControllerName);
            this.listView1.Items.Add(item);
            item.Tag = controllerInfo;
        }

        private void HandleLostEvent(object sender, NetworkWatcherEventArgs e)
        {
            // Handle the event when a controller is lost
            ControllerInfo lostController = e.Controller;
            // Update your UI or perform any required action
            // For example, removing the lost controller from the ListView
            foreach (ListViewItem item in listView1.Items)
            {
                if (item.SubItems[0].Text == lostController.IPAddress.ToString())
                {
                    listView1.Invoke(new Action(() => { listView1.Items.Remove(item); }));
                    break;
                }
            }
        }

        /* ListView - DoubleClick event handler */
        private void listView1_DoubleClick(object sender, EventArgs e)
        {
            ListViewItem item = this.listView1.SelectedItems[0];
            if (item.Tag != null)
            {
                ControllerInfo controllerInfo = (ControllerInfo)item.Tag;
                // Availability.Available is replaced by  ABB.Robotics.Controllers.Availability.Available
                if (controllerInfo.Availability == ABB.Robotics.Controllers.Availability.Available)
                {
                    if (this.controller != null)
                    {
                        this.controller.Logoff();
                        this.controller.Dispose();
                        this.controller = null;
                    }
                    this.controller = ControllerFactory.CreateFrom(controllerInfo);
                    this.controller.Logon(UserInfo.DefaultUser);
                }
                else
                {
                    MessageBox.Show("Selected controller not available.");
                }
            }
        }

        private void listView1_SelectedIndexChanged(object sender, EventArgs e)
        {

        }

        private void button1_Click(object sender, EventArgs e)
        {
            try
            {
                if (controller.OperatingMode == ControllerOperatingMode.Auto)
                {
                    tasks = controller.Rapid.GetTasks();
                    using (Mastership m = Mastership.Request(controller.Rapid))
                    {
                        //Perform operation
                        tasks[0].Start();
                    }
                }
                else
                {
                    MessageBox.Show(
                        "Automatic mode is required to start execution from a remote client.");
                }
            }
            catch (System.InvalidOperationException ex)
            {
                MessageBox.Show("Mastership is held by another client." + ex.Message);
            }
            catch (System.Exception ex)
            {
                MessageBox.Show("Unexpected error occurred: " + ex.Message);
            }
        }
    }
}
