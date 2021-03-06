﻿/* Copyright (c) Citrix Systems Inc. 
 * All rights reserved. 
 * 
 * Redistribution and use in source and binary forms, 
 * with or without modification, are permitted provided 
 * that the following conditions are met: 
 * 
 * *   Redistributions of source code must retain the above 
 *     copyright notice, this list of conditions and the 
 *     following disclaimer. 
 * *   Redistributions in binary form must reproduce the above 
 *     copyright notice, this list of conditions and the 
 *     following disclaimer in the documentation and/or other 
 *     materials provided with the distribution. 
 * 
 * THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND 
 * CONTRIBUTORS "AS IS" AND ANY EXPRESS OR IMPLIED WARRANTIES, 
 * INCLUDING, BUT NOT LIMITED TO, THE IMPLIED WARRANTIES OF 
 * MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE 
 * DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT HOLDER OR 
 * CONTRIBUTORS BE LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL, 
 * SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING, 
 * BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR 
 * SERVICES; LOSS OF USE, DATA, OR PROFITS; OR BUSINESS 
 * INTERRUPTION) HOWEVER CAUSED AND ON ANY THEORY OF LIABILITY, 
 * WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT (INCLUDING 
 * NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE 
 * OF THIS SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF 
 * SUCH DAMAGE.
 */

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using XenAdmin.Controls;
using XenAdmin.CustomFields;
using XenAdmin.Model;
using XenAdmin.Utils;
using XenAPI;
using XenAdmin.Core;
using XenAdmin.Dialogs;
using XenAdmin.SettingsPanels;
using XenAdmin.Network;
using XenAdmin.Commands;
using LicenseManager = XenAdmin.Dialogs.LicenseManager;


namespace XenAdmin.TabPages
{
    public partial class GeneralTabPage : BaseTabPage
    {
        private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        private static readonly Dictionary<Type, List<PDSection>> _expandedSections = new Dictionary<Type, List<PDSection>>();

        /// <summary>
        /// Set when we need to do a rebuild, but we are not visible, to queue up a rebuild.
        /// </summary>
        private bool refreshNeeded = false;

        private LicenseStatus licenseStatus;

        private List<PDSection> sections;

        public LicenseManagerLauncher LicenseLauncher { private get; set; }

        public GeneralTabPage()
        {
            InitializeComponent();

            VM_guest_metrics_CollectionChangedWithInvoke =
                Program.ProgramInvokeHandler(VM_guest_metrics_CollectionChanged);
            OtherConfigAndTagsWatcher.TagsChanged += new EventHandler(OtherConfigAndTagsWatcher_TagsChanged);
            sections = new List<PDSection>();
            foreach (Control control in panel2.Controls)
            {
                Panel p = control as Panel;
                if (p == null)
                    continue;

                foreach (Control c in p.Controls)
                {
                    PDSection s = c as PDSection;
                    if (s == null)
                        continue;
                    sections.Add(s);
                    s.MaximumSize = new Size(900, 9999999);
                    s.fixFirstColumnWidth(150);
                    s.contentChangedSelection += s_contentChangedSelection;
                    s.contentReceivedFocus += s_contentReceivedFocus;
                }
            }            
        }

        private void licenseStatus_ItemUpdated(object sender, EventArgs e)
        {
            if (pdSectionLicense == null)
                return;

            GeneralTabLicenseStatusStringifier ss = new GeneralTabLicenseStatusStringifier(licenseStatus);
            Program.Invoke(Program.MainWindow, () => pdSectionLicense.UpdateEntryValueWithKey(
                                                       FriendlyName("host.license_params-expiry"),
                                                       ss.ExpiryDate));

            Program.Invoke(Program.MainWindow, () => pdSectionLicense.UpdateEntryValueWithKey(
                                           Messages.LICENSE_STATUS,
                                           ss.ExpiryStatus));
        }

        void s_contentReceivedFocus(PDSection s)
        {
            scrollToSelectionIfNeeded(s);
        }

        void s_contentChangedSelection(PDSection s)
        {
            scrollToSelectionIfNeeded(s);
        }

        private void scrollToSelectionIfNeeded(PDSection s)
        {
            if (s.HasNoSelection())
                return;

            Rectangle selectedRowBounds = s.SelectedRowBounds;

            // translate to the coordinates of the pdsection container panel (the one added for padding purposes)
            selectedRowBounds.Offset(s.Parent.Location);

            // Top edge visible?
            if (panel2.ClientRectangle.Height - selectedRowBounds.Top > 0 && selectedRowBounds.Top > 0)
            {
                // Bottom edge visible?
                if (panel2.ClientRectangle.Height - selectedRowBounds.Bottom > 0 && selectedRowBounds.Bottom > 0)
                {
                    // The entire selected row is in view, no need to move 
                    return;
                }
            }

            panel2.ForceScrollTo(s);
        }

        private void EditButton_Click(object sender, EventArgs e)
        {
            new PropertiesCommand(Program.MainWindow.CommandInterface, xenObject).Execute();
        }

        private IXenObject xenObject;
        public IXenObject XenObject
        {
            set
            {
                SetupAnStartLicenseStatus(value);
                if (xenObject != value)
                {
                    UnregisterHandlers();

                    // special case for StorageLinkRepository, use its SR in this case.

                    StorageLinkRepository slr = value as StorageLinkRepository;
                    SR sr = slr != null ? slr.SR(ConnectionsManager.XenConnectionsCopy) : null;

                    xenObject = sr ?? value;
                    RegisterHandlers();
                    BuildList();
                    List<PDSection> listPDSections = null;
                    if (_expandedSections.TryGetValue(xenObject.GetType(), out listPDSections))
                        ResetExpandState(listPDSections);
                    else
                        ResetExpandState();
                }
                else
                {
                    BuildList();
                }
            }
        }

        private void SetupAnStartLicenseStatus(IXenObject xo)
        {
            licenseStatus = new LicenseStatus(xo);
            licenseStatus.ItemUpdated += licenseStatus_ItemUpdated;
            licenseStatus.BeginUpdate();
        }

        void s_ExpandedEventHandler(PDSection pdSection)
        {
            if (pdSection != null)
            {
                //Add to the expandedSections
                List<PDSection> listSections;
                if (_expandedSections.TryGetValue(xenObject.GetType(), out listSections))
                {
                    if (!listSections.Contains(pdSection) && pdSection.IsExpanded)
                        listSections.Add(pdSection);
                    else if (!pdSection.IsExpanded)
                        listSections.Remove(pdSection);
                }
                else if (pdSection.IsExpanded)
                {
                    List<PDSection> list = new List<PDSection>();
                    list.Add(pdSection);
                    _expandedSections.Add(xenObject.GetType(), list);
                }
            }
            SetStatesOfExpandingLinks();
        }

        private void ResetExpandState()
        {
            panel2.SuspendLayout();
            foreach (PDSection s in sections)
            {
                s.Contract();
            }
            pdSectionGeneral.Expand();
            panel2.ResumeLayout();
        }
        private void ResetExpandState(List<PDSection> expandedSections)
        {
            panel2.SuspendLayout();
            foreach (PDSection s in sections)
            {
                if (expandedSections.Contains(s))
                    s.Expand();
                else
                    s.Contract();
            }
            pdSectionGeneral.Expand();
            panel2.ResumeLayout();
        }

        private void UnregisterHandlers()
        {
            if (xenObject != null)
                xenObject.PropertyChanged -= PropertyChanged;

            if (xenObject is Host)
            {
                Host host = xenObject as Host;

                Host_metrics metric = xenObject.Connection.Resolve<Host_metrics>(host.metrics);
                if (metric != null)
                    metric.PropertyChanged -= PropertyChanged;
            }
            else if (xenObject is VM)
            {
                VM vm = xenObject as VM;

                VM_metrics metric = vm.Connection.Resolve(vm.metrics);
                if (metric != null)
                    metric.PropertyChanged -= PropertyChanged;

                VM_guest_metrics guestmetric = xenObject.Connection.Resolve(vm.guest_metrics);
                if (guestmetric != null)
                    guestmetric.PropertyChanged -= PropertyChanged;

                vm.Connection.Cache.DeregisterCollectionChanged<VM_guest_metrics>(VM_guest_metrics_CollectionChangedWithInvoke);
            }
            else if (xenObject is SR)
            {
                SR sr = xenObject as SR;

                foreach (PBD pbd in sr.Connection.ResolveAll(sr.PBDs))
                {
                    pbd.PropertyChanged -= PropertyChanged;
                }
            }
            else if (xenObject is Pool)
            {
                xenObject.Connection.Cache.DeregisterBatchCollectionChanged<Pool_patch>(Pool_patch_BatchCollectionChanged);
            }
        }

        void VM_guest_metrics_CollectionChanged(object sender, CollectionChangeEventArgs e)
        {
            if (!this.Visible)
                return;
            // Required to refresh the panel when the vm boots so we show the correct pv driver state and version
            // Note this does NOT get called every 2s, just when the vm power state changes (hopefully!)
            BuildList();
        }

        private readonly CollectionChangeEventHandler VM_guest_metrics_CollectionChangedWithInvoke;
        private void RegisterHandlers()
        {
            if (xenObject != null)
                xenObject.PropertyChanged += new PropertyChangedEventHandler(PropertyChanged);

            if (xenObject is Host)
            {
                Host host = xenObject as Host;
                Host_metrics metric = xenObject.Connection.Resolve(host.metrics);
                if (metric != null)
                    metric.PropertyChanged += new PropertyChangedEventHandler(PropertyChanged);
            }
            else if (xenObject is VM)
            {
                VM vm = xenObject as VM;

                VM_metrics metric = vm.Connection.Resolve(vm.metrics);
                if (metric != null)
                    metric.PropertyChanged += new PropertyChangedEventHandler(PropertyChanged);

                VM_guest_metrics guestmetric = xenObject.Connection.Resolve(vm.guest_metrics);
                if (guestmetric != null)
                    guestmetric.PropertyChanged += new PropertyChangedEventHandler(PropertyChanged);

                xenObject.Connection.Cache.RegisterCollectionChanged<VM_guest_metrics>(VM_guest_metrics_CollectionChangedWithInvoke);
            }
            else if (xenObject is Pool)
            {
                xenObject.Connection.Cache.RegisterBatchCollectionChanged<Pool_patch>(Pool_patch_BatchCollectionChanged);
            }
        }

        void Pool_patch_BatchCollectionChanged(object sender, EventArgs e)
        {
            Program.BeginInvoke(this, BuildList);
        }

        void OtherConfigAndTagsWatcher_TagsChanged(object sender, EventArgs e)
        {
            BuildList();
        }

        // We queue up a rebuild if we are not shown but the contents becomes out of date, this just fires off the rebuild
        protected override void OnVisibleChanged(EventArgs e)
        {
            if (Visible && refreshNeeded)
            {
                BuildList();
                refreshNeeded = false;
            }
            base.OnVisibleChanged(e);
        }

        private void PropertyChanged(object sender, PropertyChangedEventArgs e)
        {

            if (e.PropertyName == "state" ||
                e.PropertyName == "last_updated")
            {
                return;
            }

            Program.Invoke(this, delegate
            {
                if (e.PropertyName == "PBDs")
                {
                    SR sr = xenObject as SR;
                    if (sr == null)
                        return;

                    foreach (PBD pbd in xenObject.Connection.ResolveAll(sr.PBDs))
                    {
                        pbd.PropertyChanged -= PropertyChanged;
                        pbd.PropertyChanged += PropertyChanged;
                    }

                    BuildList();
                }
                else
                {
                    // Atm we are rebuilding on almost any property changed event. 
                    // As long as we are just clearing and readding the rows in the PDSections this seems to be super quick. 
                    // If it gets slower we should update specific boxes for specific property changes.
                    if (licenseStatus.Updated)
                        licenseStatus.BeginUpdate();
                    BuildList();
                    EnableDisableEdit();
                }
            });
        }

        public void EnableDisableEdit()
        {
            buttonProperties.Enabled = xenObject != null && !xenObject.Locked && xenObject.Connection != null && xenObject.Connection.IsConnected;
        }

        public void BuildList()
        {
            //Program.AssertOnEventThread();

            if (!this.Visible)
            {
                refreshNeeded = true;
                return;
            }
            if (xenObject == null)
                return;

            if (xenObject is Host && !xenObject.Connection.IsConnected)
                base.Text = Messages.CONNECTION_GENERAL_TAB_TITLE;
            else if (xenObject is Host)
            {
                base.Text = Messages.HOST_GENERAL_TAB_TITLE;
            }
                
                
            else if (xenObject is VM)
            {
                VM vm = (VM)xenObject;
                if (vm.is_a_snapshot)
                    base.Text = Messages.SNAPSHOT_GENERAL_TAB_TITLE;
                else if (vm.is_a_template)
                    base.Text = Messages.TEMPLATE_GENERAL_TAB_TITLE;
                else
                    base.Text = Messages.VM_GENERAL_TAB_TITLE;
            }
            else if (xenObject is SR)
                base.Text = Messages.SR_GENERAL_TAB_TITLE;
            else if (xenObject is Pool)
                base.Text = Messages.POOL_GENERAL_TAB_TITLE;
            else if (xenObject is StorageLinkPool)
                base.Text = Messages.STORAGELINKPOOL_GENERAL_TAB_TITLE;
            else if (xenObject is StorageLinkServer)
                base.Text = Messages.STORAGELINKSERVER_GENERAL_TAB_TITLE;
            else if (xenObject is StorageLinkSystem)
                base.Text = Messages.STORAGELINKSYSTEM_GENERAL_TAB_TITLE;
            else if (xenObject is StorageLinkRepository)
                base.Text = Messages.SR_GENERAL_TAB_TITLE;

            panel2.SuspendLayout();
            // Clear all the data from the sections (visible and non visible)
            foreach (PDSection s in sections)
            {
                s.PauseLayout();
                s.ClearData();
            }
            // Generate the content of each box, each method performs a cast and only populates if XenObject is the relevant type
           
            if (xenObject is Host && (xenObject.Connection == null || !xenObject.Connection.IsConnected))
            {
                generateDisconnectedHostBox();
            }
            else
            {
                generateGeneralBox();
                generateCustomFieldsBox();
                generateInterfaceBox();
                generateMemoryBox();
                generateVersionBox();
                generateLicenseBox();
                generateCPUBox();
                generateHostPatchesBox();
                generateBootBox();
                generateHABox();
                generateStatusBox();
                generateMultipathBox();
                generatePoolPatchesBox();
                generateStorageLinkBox();
                generateStorageLinkSystemCapabilitiesBox();
                generateMultipathBootBox();
            }

            // hide all the sections which haven't been populated, those that have make sure are visible
            foreach (PDSection s in sections)
            {
                if (s.IsEmpty())
                {
                    s.Parent.Visible = false;
                }
                else
                {
                    s.Parent.Visible = true;
                    if (s.ContainsFocus)
                        s.RestoreSelection();
                }
                s.StartLayout();
            }
            panel2.ResumeLayout();
            EnableDisableEdit();
        }

        private void generateInterfaceBox()
        {
            Host Host = xenObject as Host;
            Pool Pool = xenObject as Pool;
            if (Host != null)
            {
                fillInterfacesForHost(Host, false);
            }
            else if (Pool != null)
            {
                // Here we tell fillInterfacesForHost to prefix each entry with the hosts name label, so we know which entry belongs to which host
                // and also to better preserve uniqueness for keys in the PDSection

                foreach (Host h in Pool.Connection.Cache.Hosts)
                    fillInterfacesForHost(h, true);
            }
        }

        private void fillInterfacesForHost(Host Host, bool includeHostSuffix)
        {
            PDSection s = pdSectionManagementInterfaces;

            ToolStripMenuItem editValue = MainWindow.NewToolStripMenuItem(Messages.EDIT, Properties.Resources.edit_16, delegate(object sender, EventArgs e)
            {
                NetworkingProperties p = new NetworkingProperties(Host, null);
                p.ShowDialog(Program.MainWindow);
            });
            List<ToolStripMenuItem> menuItems = new List<ToolStripMenuItem>();
            menuItems.Add(editValue);

            if (!string.IsNullOrEmpty(Host.hostname))
            {
                if (!includeHostSuffix)
                    s.AddEntry(FriendlyName("host.hostname"), Host.hostname, menuItems);
                else
                    s.AddEntry(
                        string.Format(Messages.PROPERTY_ON_OBJECT, FriendlyName("host.hostname"), Helpers.GetName(Host)),
                        Host.hostname,
                        menuItems);
            }
            foreach (PIF pif in xenObject.Connection.ResolveAll<PIF>(Host.PIFs))
            {
                if (pif.management)
                {
                    if (!includeHostSuffix)
                        s.AddEntry(Messages.MANAGEMENT_INTERFACE, pif.FriendlyIPAddress, menuItems);
                    else
                        s.AddEntry(
                            string.Format(Messages.PROPERTY_ON_OBJECT, Messages.MANAGEMENT_INTERFACE, Helpers.GetName(Host)),
                            pif.FriendlyIPAddress,
                            menuItems);
                }
            }

            foreach (PIF pif in xenObject.Connection.ResolveAll<PIF>(Host.PIFs))
            {
                if (pif.IsSecondaryManagementInterface(Properties.Settings.Default.ShowHiddenVMs))
                {
                    if (!includeHostSuffix)
                        s.AddEntry(pif.ManagementPurpose.Ellipsise(30), pif.FriendlyIPAddress, menuItems);
                    else
                        s.AddEntry(
                            string.Format(Messages.PROPERTY_ON_OBJECT, pif.ManagementPurpose.Ellipsise(30), Helpers.GetName(Host)),
                            pif.FriendlyIPAddress,
                            menuItems);
                }
            }
        }

        private void generateCustomFieldsBox()
        {
            List<CustomField> customFields = CustomFieldsManager.CustomFieldValues(xenObject);
            if (customFields.Count <= 0)
                return;

            PDSection s = pdSectionCustomFields;

            foreach (CustomField customField in customFields)
            {
                ToolStripMenuItem editValue = MainWindow.NewToolStripMenuItem(Messages.EDIT, Properties.Resources.edit_16, delegate(object sender, EventArgs e)
                {
                    PropertiesDialog dialog = new PropertiesDialog(xenObject);
                    dialog.SelectPage(dialog.CustomFieldsEditPage);
                    dialog.ShowDialog();
                });
                List<ToolStripMenuItem> menuItems = new List<ToolStripMenuItem>();
                menuItems.Add(editValue);
                CustomFieldWrapper cfWrapper = new CustomFieldWrapper(xenObject, customField.Definition);

                s.AddEntry(customField.Definition.Name.Ellipsise(30), cfWrapper.ToString(), menuItems, customField.Definition.Name);
            }
        }

        private void generatePoolPatchesBox()
        {
            Pool pool = xenObject as Pool;
            if (pool == null)
                return;

            PDSection s = pdSectionUpdates;

            List<KeyValuePair<String, String>> messages = CheckPoolUpdate(pool);
            if (messages.Count > 0)
            {
                foreach (KeyValuePair<String, String> kvp in messages)
                {
                    s.AddEntry(kvp.Key, kvp.Value);
                }
            }
            Host master = Helpers.GetMaster(xenObject.Connection);
            if (master == null)
                return;

            var poolAppPatches = poolAppliedPatches();
            if (!string.IsNullOrEmpty(poolAppPatches))
            {
                s.AddEntry(FriendlyName("Pool_patch.fully_applied"), poolAppPatches);
                return;
            }

            CommandToolStripMenuItem applypatch = new CommandToolStripMenuItem(
                new InstallNewUpdateCommand(Program.MainWindow.CommandInterface), true);

            List<ToolStripMenuItem> menuItems = new List<ToolStripMenuItem>();
            menuItems.Add(applypatch);

            var poolPartPatches = poolPartialPatches();
            if (!string.IsNullOrEmpty(poolPartPatches))
            {
                s.AddEntry(FriendlyName("Pool_patch.partially_applied"), poolPartPatches, menuItems, Color.Red);
                return;
            }

            var poolNotAppPatches = poolNotAppliedPatches();
            if (!string.IsNullOrEmpty(poolNotAppPatches))
                s.AddEntry(FriendlyName("Pool_patch.not_applied"), poolNotAppPatches, menuItems, Color.Red);
        }

        private void generateHostPatchesBox()
        {
            Host host = xenObject as Host;
            if (host == null)
                return;

            PDSection s = pdSectionUpdates;

            List<KeyValuePair<String, String>> messages = CheckServerUpdates(host);
            if (messages.Count > 0)
            {
                foreach (KeyValuePair<String, String> kvp in messages)
                {
                    s.AddEntry(kvp.Key, kvp.Value);
                }
            }
            if (hostAppliedPatches(host) != "")
            {
                s.AddEntry(FriendlyName("Pool_patch.applied"), hostAppliedPatches(host));
            }
            if (!Host.IsFullyPatched(host, ConnectionsManager.XenConnectionsCopy))
            {
                CommandToolStripMenuItem applypatch =
                           new CommandToolStripMenuItem(
                               new InstallNewUpdateCommand(Program.MainWindow.CommandInterface), true);

                List<ToolStripMenuItem> menuItems = new List<ToolStripMenuItem>();
                menuItems.Add(applypatch);
                s.AddEntry(FriendlyName("Pool_patch.not_applied"), hostUnappliedPatches(host), menuItems, Color.Red);
            }
        }

        private void generateHABox()
        {
            VM vm = xenObject as VM;
            if (vm == null)
                return;

            Pool pool = Helpers.GetPoolOfOne(xenObject.Connection);
            if (pool == null || !pool.ha_enabled)
                return;

            PDSection s = pdSectionHighAvailability;

            List<ToolStripMenuItem> menuItems = new List<ToolStripMenuItem>();
            menuItems.Add(EditMenuItem("VMHAEditPage", "comboBoxProtectionLevel"));

            s.AddEntry(FriendlyName("VM.ha_restart_priority"), Helpers.RestartPriorityI18n(vm.HARestartPriority), menuItems);
        }

      

        private void generateStatusBox()
        {
            SR sr = xenObject as SR;
            if (sr == null)
                return;

            PDSection s = pdSectionStatus;

            bool broken = sr.IsBroken() || !sr.MultipathAOK || sr.NeedsUpgrading;
            bool detached = !sr.HasPBDs;

            List<ToolStripMenuItem> menuItems = new List<ToolStripMenuItem>();
            ToolStripMenuItem repair = MainWindow.NewToolStripMenuItem(sr.NeedsUpgrading ? Messages.UPGRADE_SR : Messages.GENERAL_SR_CONTEXT_REPAIR,
                Properties.Resources._000_StorageBroken_h32bit_16,
                delegate(object sender, EventArgs e)
                {
                    if (sr.NeedsUpgrading)
                    {
                        new UpgradeSRCommand(Program.MainWindow.CommandInterface, sr).Execute();
                    }
                    else
                        Program.MainWindow.ShowPerConnectionWizard(xenObject.Connection, new RepairSRDialog(sr));
                });
            menuItems.Add(repair);

            if (broken && !detached)
                s.AddEntry(FriendlyName("SR.state"), sr.StatusString, menuItems);
            else
                s.AddEntry(FriendlyName("SR.state"), sr.StatusString);

            foreach (Host host in xenObject.Connection.Cache.Hosts)
            {
                PBD pbdToSR = null;
                foreach (PBD pbd in xenObject.Connection.ResolveAll(host.PBDs))
                {
                    if (pbd.SR.opaque_ref != xenObject.opaque_ref)
                        continue;

                    pbdToSR = pbd;
                    break;
                }
                if (pbdToSR == null)
                {
                    if (!sr.shared)
                        continue;

                    if (!detached)
                        s.AddEntry("  " + Helpers.GetName(host).Ellipsise(30),
                            Messages.REPAIR_SR_DIALOG_CONNECTION_MISSING, menuItems, Color.Red);
                    else
                        s.AddEntry("  " + Helpers.GetName(host).Ellipsise(30),
                            Messages.REPAIR_SR_DIALOG_CONNECTION_MISSING, Color.Red);

                    continue;
                }

                pbdToSR.PropertyChanged -= new PropertyChangedEventHandler(PropertyChanged);
                pbdToSR.PropertyChanged += new PropertyChangedEventHandler(PropertyChanged);

                if (!pbdToSR.currently_attached)
                {
                    if (!detached)
                        s.AddEntry(Helpers.GetName(host).Ellipsise(30), pbdToSR.StatusString, menuItems, Color.Red);
                    else
                        s.AddEntry(Helpers.GetName(host).Ellipsise(30), pbdToSR.StatusString, Color.Red);
                }
                else
                {
                    s.AddEntry(Helpers.GetName(host).Ellipsise(30), pbdToSR.StatusString);
                }
            }
        }

        private void generateMultipathBox()
        {
            SR sr = xenObject as SR;
            if (sr == null)
                return;

            PDSection s = pdSectionMultipathing;

            if (!sr.MultipathCapable)
            {
                s.AddEntry(Messages.MULTIPATH_CAPABLE, Messages.NO);
                return;
            }

            if (sr.LunPerVDI)
            {
                Dictionary<VM, Dictionary<VDI, String>>
                    pathStatus = sr.GetMultiPathStatusLunPerVDI();

                foreach (Host host in xenObject.Connection.Cache.Hosts)
                {
                    PBD pbd = sr.GetPBDFor(host);
                    if (pbd == null || !pbd.MultipathActive)
                    {
                        s.AddEntry(host.Name, Messages.MULTIPATH_NOT_ACTIVE);
                        continue;
                    }

                    s.AddEntry(host.Name, Messages.MULTIPATH_ACTIVE);
                    foreach (KeyValuePair<VM, Dictionary<VDI, String>> kvp in pathStatus)
                    {
                        VM vm = kvp.Key;
                        if (vm.resident_on == null ||
                            vm.resident_on.opaque_ref != host.opaque_ref)
                            continue;

                        bool renderOnOneLine = false;
                        int lastMax = -1;
                        int lastCurrent = -1;

                        foreach (KeyValuePair<VDI, String> kvp2 in kvp.Value)
                        {
                            int current;
                            int max;
                            if (!PBD.ParsePathCounts(kvp2.Value, out current, out max))
                                continue;

                            if (!renderOnOneLine)
                            {
                                lastMax = max;
                                lastCurrent = current;
                                renderOnOneLine = true;
                                continue;
                            }

                            if (lastMax == max && lastCurrent == current)
                                continue;

                            renderOnOneLine = false;
                            break;
                        }

                        if (renderOnOneLine)
                        {
                            AddMultipathLine(s, String.Format("    {0}", vm.Name),
                                             lastCurrent, lastMax, pbd.ISCSISessions);
                        }
                        else
                        {
                            s.AddEntry(String.Format("    {0}", vm.Name), "");

                            foreach (KeyValuePair<VDI, String> kvp2 in kvp.Value)
                            {
                                int current;
                                int max;
                                if (!PBD.ParsePathCounts(kvp2.Value, out current, out max))
                                    continue;

                                AddMultipathLine(s, String.Format("        {0}", kvp2.Key.Name),
                                                current, max, pbd.ISCSISessions);
                            }
                        }
                    }
                }
            }
            else
            {
                Dictionary<PBD, String> pathStatus = sr.GetMultiPathStatusLunPerSR();

                foreach (Host host in xenObject.Connection.Cache.Hosts)
                {
                    PBD pbd = sr.GetPBDFor(host);
                    if (pbd == null || !pathStatus.ContainsKey(pbd))
                    {
                        s.AddEntry(host.Name,
                            pbd != null && pbd.MultipathActive ?
                            Messages.MULTIPATH_ACTIVE :
                            Messages.MULTIPATH_NOT_ACTIVE);
                        continue;
                    }

                    String status = pathStatus[pbd];

                    int current;
                    int max;
                    PBD.ParsePathCounts(status, out current, out max); //Guaranteed to work if PBD is in pathStatus

                    AddMultipathLine(s, host.Name, current, max, pbd.ISCSISessions);
                }
            }
        }

        private void AddMultipathLine(PDSection s, String title, int current, int max, int iscsiSessions)
        {
            bool bad = current < max || (iscsiSessions != -1 && max < iscsiSessions);
            string row = string.Format(Messages.MULTIPATH_STATUS, current, max);
            if (iscsiSessions != -1)
                row += string.Format(Messages.MULTIPATH_STATUS_ISCSI_SESSIONS, iscsiSessions);

            if (bad)
                s.AddEntry(title, row, Color.Red);
            else
                s.AddEntry(title, row);
        }

        private void generateMultipathBootBox()
        {
            Host host = xenObject as Host;
            if (host == null)
                return;

            int current, max;
            if (!host.GetBootPathCounts(out current, out max))
                return;

            PDSection s = pdSectionMultipathBoot;
            string text = string.Format(Messages.MULTIPATH_STATUS, current, max);
            bool bad = current < max;
            if (bad)
                s.AddEntry(Messages.STATUS, text, Color.Red);
            else
                s.AddEntry(Messages.STATUS, text);
        }

        private void generateBootBox()
        {
            VM vm = xenObject as VM;
            if (vm == null)
                return;

            PDSection s = pdSectionBootOptions;

            List<ToolStripMenuItem> menuItems = new List<ToolStripMenuItem>();

			if (!Helpers.BostonOrGreater(vm.Connection))
			{
				menuItems.Add(EditMenuItem("StartupOptionsEditPage", "ckbAutoBoot"));
				s.AddEntry(FriendlyName("VM.auto_boot"), Helpers.BoolToString(vm.AutoPowerOn), menuItems);
				menuItems.Clear();
			}

        	if (vm.IsHVM)
            {	
                menuItems.Add(EditMenuItem("StartupOptionsEditPage", "lstOrder"));
                s.AddEntry(FriendlyName("VM.BootOrder"), HVMBootOrder(vm), menuItems);
            }
            else
            {
                menuItems.Add(EditMenuItem("StartupOptionsEditPage", "txtOSParams"));
                s.AddEntry(FriendlyName("VM.PV_args"), vm.PV_args, menuItems);
            }
        }

        private void generateLicenseBox()
        {
            Host host = xenObject as Host;
            if (host == null)
                return;

            PDSection s = pdSectionLicense;

            if (host.license_params == null || host.IsXCP)
                return;

            Dictionary<string, string> info = new Dictionary<string, string>(host.license_params);

            // This field is now supressed as it has no meaning under the current license scheme, and was never
            // enforced anyway.
            info.Remove("sockets");

            if (info.ContainsKey("expiry"))
            {
                ToolStripMenuItem editItem = MainWindow.NewToolStripMenuItem(Messages.LAUNCH_LICENSE_MANAGER, delegate
                {
                    if(LicenseLauncher != null)
                    {
                        LicenseLauncher.Parent = Program.MainWindow;
                        LicenseLauncher.LaunchIfRequired(false, ConnectionsManager.XenConnections);
                    }
                        
                });

                GeneralTabLicenseStatusStringifier ss = new GeneralTabLicenseStatusStringifier(licenseStatus);
                s.AddEntry(Messages.LICENSE_STATUS, ss.ExpiryStatus, new List<ToolStripMenuItem>(new [] { editItem }));
                s.AddEntry(FriendlyName("host.license_params-expiry"), ss.ExpiryDate, new List<ToolStripMenuItem>(new ToolStripMenuItem[] { editItem }));
                info.Remove("expiry");
            }

            if (!string.IsNullOrEmpty(host.edition))
            {
                s.AddEntry(FriendlyName("host.edition"), FriendlyName(String.Format("host.edition-{0}", host.edition)) ?? String.Empty);
                if (info.ContainsKey("sku_type"))
                {
                    info.Remove("sku_type");
                }
            }
            else if (info.ContainsKey("sku_type"))
            {
                s.AddEntry(FriendlyName("host.license_params-sku_type"), Helpers.GetFriendlyLicenseName(host));
                info.Remove("sku_type");
            }

            if(Helpers.ClearwaterOrGreater(host))
                s.AddEntry(Messages.NUMBER_OF_SOCKETS, host.CpuSockets.ToString());

            if (host.license_server.ContainsKey("address"))
            {
                s.AddEntry(FriendlyName(String.Format("host.license_server-address")), host.license_server["address"]);
            }
            if (host.license_server.ContainsKey("port"))
            {
                s.AddEntry(FriendlyName(String.Format("host.license_server-port")), host.license_server["port"]);
            }
            if (host.software_version.ContainsKey("dbv"))
            {
                s.AddEntry("DBV", host.software_version["dbv"]);
            }

            foreach (string key in new string[] { "productcode", "serialnumber" })
            {
                if (info.ContainsKey(key))
                {
                    string row_name = string.Format("host.license_params-{0}", key);
                    string k = key;
                    if (host.license_params[k] != string.Empty)
                        s.AddEntry(FriendlyName(row_name), host.license_params[k]);
                    info.Remove(key);
                }
            }

            string restrictions = Helpers.GetHostRestrictions(host);
            if (restrictions != "")
            {
                s.AddEntry(Messages.RESTRICTIONS, restrictions);
            }
        }

        private void generateVersionBox()
        {
            Host host = xenObject as Host;

            if (host == null || host.software_version == null)
                return;

            bool isXCP = host.IsXCP;
            if (host.software_version.ContainsKey("date"))
                pdSectionVersion.AddEntry(isXCP ? Messages.SOFTWARE_VERSION_XCP_DATE : Messages.SOFTWARE_VERSION_DATE, host.software_version["date"]);
            if (host.software_version.ContainsKey("build_number"))
                pdSectionVersion.AddEntry(isXCP ? Messages.SOFTWARE_VERSION_XCP_BUILD_NUMBER : Messages.SOFTWARE_VERSION_BUILD_NUMBER, host.software_version["build_number"]);
            if (isXCP && host.software_version.ContainsKey("platform_version"))
                pdSectionVersion.AddEntry(Messages.SOFTWARE_VERSION_XCP_PLATFORM_VERSION, host.software_version["platform_version"]);
            if (!isXCP && host.software_version.ContainsKey("product_version"))
                pdSectionVersion.AddEntry(Messages.SOFTWARE_VERSION_PRODUCT_VERSION, host.ProductVersionText);
        }

        private void generateCPUBox()
        {
            Host host = xenObject as Host;
            if (host == null)
                return;

            PDSection s = pdSectionCPU;

            SortedDictionary<long, Host_cpu> d = new SortedDictionary<long, Host_cpu>();
            foreach (Host_cpu cpu in xenObject.Connection.ResolveAll(host.host_CPUs))
            {
                d.Add(cpu.number, cpu);
            }

            bool cpusIdentical = CPUsIdentical(d.Values);

            foreach (Host_cpu cpu in d.Values)
            {
                String label = String.Format(Messages.GENERAL_DETAILS_CPU_NUMBER,
                    cpusIdentical && d.Values.Count > 1 ? String.Format("0 - {0}", d.Values.Count - 1)
                        : cpu.number.ToString());

                s.AddEntry(label, Helpers.GetCPUProperties(cpu));
                if (cpusIdentical)
                    break;
            }
        }



        private void generateDisconnectedHostBox()
        {
            IXenConnection conn = xenObject.Connection;

            PDSection s = pdSectionGeneral;

            string name = Helpers.GetName(xenObject);
            s.AddEntry(FriendlyName("host.name_label"), name);
            if (conn != null && conn.Hostname != name)
                s.AddEntry(FriendlyName("host.address"), conn.Hostname);

            if (conn != null && conn.PoolMembers.Count > 1)
                s.AddEntry(FriendlyName("host.pool_members"), string.Join(", ", conn.PoolMembers.ToArray()));

        }

        private void generateGeneralBox()
        {
            PDSection s = pdSectionGeneral;

            s.AddEntry(FriendlyName("host.name_label"), Helpers.GetName(xenObject),
                new List<ToolStripMenuItem>(new ToolStripMenuItem[] { EditMenuItem("GeneralEditPage", "txtName") }));

            if (!(xenObject is IStorageLinkObject))
            {
                VM vm = xenObject as VM;
                if (vm == null || vm.DescriptionType != VM.VmDescriptionType.None)
                {
                    s.AddEntry(FriendlyName("host.name_description"), xenObject.Description,
                               new List<ToolStripMenuItem>(new[] {EditMenuItem("GeneralEditPage", "txtDescription")}));
                }

                GenTagRow(s);
                GenFolderRow(s);
            }

            if (xenObject is Host)
            {
                Host host = xenObject as Host;

                if (Helpers.GetPool(xenObject.Connection) != null)
                    s.AddEntry(Messages.POOL_MASTER, host.IsMaster() ? Messages.YES : Messages.NO);

                if (!host.IsLive)
                {
                    s.AddEntry(FriendlyName("host.enabled"), Messages.HOST_NOT_LIVE, Color.Red);
                }
                else if (!host.enabled)
                {
                    s.AddEntry(FriendlyName("host.enabled"),
                               host.MaintenanceMode ? Messages.HOST_IN_MAINTENANCE_MODE : Messages.DISABLED,
                                 new List<ToolStripMenuItem>(new ToolStripMenuItem[] {
                                    MainWindow.NewToolStripMenuItem(Messages.EXIT_MAINTENANCE_MODE, delegate(object sender, EventArgs e)
                                    {
                                        new HostMaintenanceModeCommand(Program.MainWindow.CommandInterface, host, HostMaintenanceModeCommandParameter.Exit).Execute();
                                    })
                                 }),
                               Color.Red);
                }
                else
                {
                    s.AddEntry(FriendlyName("host.enabled"), Messages.YES,
                                 new List<ToolStripMenuItem>(new ToolStripMenuItem[] {
                                    MainWindow.NewToolStripMenuItem(Messages.ENTER_MAINTENANCE_MODE, delegate(object sender, EventArgs e)
                                    {
                                        new HostMaintenanceModeCommand(Program.MainWindow.CommandInterface, host, HostMaintenanceModeCommandParameter.Enter).Execute();
                                    })
                                 }));
                }

                s.AddEntry(FriendlyName("host.iscsi_iqn"), host.iscsi_iqn,
                    new List<ToolStripMenuItem>(new ToolStripMenuItem[] { EditMenuItem("GeneralEditPage", "txtIQN") }));
                s.AddEntry(FriendlyName("host.log_destination"), host.SysLogDestination ?? Messages.HOST_LOG_DESTINATION_LOCAL,
                    new List<ToolStripMenuItem>(new ToolStripMenuItem[] { EditMenuItem("LogDestinationEditPage", "localRadioButton") }));

                PrettyTimeSpan uptime = host.Uptime;
                PrettyTimeSpan agentUptime = host.AgentUptime;
                s.AddEntry(FriendlyName("host.uptime"), uptime == null ? "" : host.Uptime.ToString());
                s.AddEntry(FriendlyName("host.agentUptime"), agentUptime == null ? "" : host.AgentUptime.ToString());

                if ((Helpers.GeorgeOrGreater(xenObject.Connection) && host.external_auth_type == Auth.AUTH_TYPE_AD))
                    s.AddEntry(FriendlyName("host.external_auth_service_name"), host.external_auth_service_name);
            }
            else if (xenObject is VM)
            {
                VM vm = xenObject as VM;

                s.AddEntry(FriendlyName("VM.OSName"), vm.GetOSName());

                if (!vm.DefaultTemplate && Helpers.MidnightRideOrGreater(vm.Connection))
                {
                    s.AddEntry(Messages.BIOS_STRINGS_COPIED, vm.BiosStringsCopied ? Messages.YES : Messages.NO);
                }

				if (Helpers.BostonOrGreater(vm.Connection) && vm.Connection != null)
				{
					var appl = vm.Connection.Resolve(vm.appliance);
					if (appl != null)
					{
						ToolStripMenuItem applProperties = MainWindow.NewToolStripMenuItem(Messages.VM_APPLIANCE_PROPERTIES,
																							 (sender, e) =>
																							 {
																								 using (PropertiesDialog propertiesDialog = new PropertiesDialog(appl))
																								 	propertiesDialog.ShowDialog(this);
																							 });

						s.AddEntryLink(Messages.VM_APPLIANCE, appl.Name, new List<ToolStripMenuItem>(new[] { applProperties }),
									   () =>
									   {
										   using (PropertiesDialog propertiesDialog = new PropertiesDialog(appl))
											   propertiesDialog.ShowDialog(this);
									   });
					}
				}


            	if (vm.is_a_snapshot)
                {
                    VM snapshotOf = vm.Connection.Resolve(vm.snapshot_of);
                    s.AddEntry(Messages.SNAPSHOT_OF, snapshotOf == null ? string.Empty : snapshotOf.Name);
                    s.AddEntry(Messages.CREATION_TIME, HelpersGUI.DateTimeToString(vm.snapshot_time.ToLocalTime() + vm.Connection.ServerTimeOffset, Messages.DATEFORMAT_DMY_HMS, true));
                }

                if (!vm.is_a_template)
                {
                    if (vm.power_state == vm_power_state.Running)
                    {
                        if (vm.virtualisation_status == VM.VirtualisationStatus.PV_DRIVERS_NOT_INSTALLED
                            || vm.virtualisation_status == VM.VirtualisationStatus.PV_DRIVERS_OUT_OF_DATE)
                        {
                            if (InstallToolsCommand.CanExecute(vm))
                            {
                                ToolStripMenuItem installtools = MainWindow.NewToolStripMenuItem(
                                    Messages.INSTALL_XENSERVER_TOOLS_DOTS, delegate(object sender, EventArgs e)
                                    {
                                        new InstallToolsCommand(Program.MainWindow.CommandInterface, vm).Execute();
                                    });
                                s.AddEntryLink(FriendlyName("VM.VirtualizationState"), vm.VirtualisationStatusString,
                                    new List<ToolStripMenuItem>(new ToolStripMenuItem[] { installtools }), new InstallToolsCommand(Program.MainWindow.CommandInterface, vm));
                            }
                            else
                            {
                                s.AddEntry(FriendlyName("VM.VirtualizationState"), vm.VirtualisationStatusString, Color.Red);
                            }

                        }
                        else
                        {
                            s.AddEntry(FriendlyName("VM.VirtualizationState"), vm.VirtualisationStatusString);
                        }
                    }

                    if (vm.RunningTime != null)
                        s.AddEntry(FriendlyName("VM.uptime"), vm.RunningTime.ToString());

                    if (vm.IsP2V)
                    {
                        s.AddEntry(FriendlyName("VM.P2V_SourceMachine"), vm.P2V_SourceMachine);
                        s.AddEntry(FriendlyName("VM.P2V_ImportDate"), HelpersGUI.DateTimeToString(vm.P2V_ImportDate.ToLocalTime(), Messages.DATEFORMAT_DMY_HMS, true));
                    }

                    // Dont show if WLB is enabled.
                    if (VMCanChooseHomeServer(vm))
                    {
                        s.AddEntry(FriendlyName("VM.affinity"), vm.AffinityServerString,
                            new List<ToolStripMenuItem>(new ToolStripMenuItem[] { EditMenuItem("HomeServerPage", "picker") }));
                    }
                }
            }
            else if (xenObject is XenObject<SR>)
            {
                SR sr = xenObject as SR;
                s.AddEntry(Messages.TYPE, sr.FriendlyTypeName);

                if (sr.content_type != SR.Content_Type_ISO && sr.GetSRType(false) != SR.SRTypes.udev)
                    s.AddEntry(FriendlyName("SR.size"), sr.SizeString);

                if (sr.GetScsiID() != null)
                    s.AddEntry(FriendlyName("SR.scsiid"), sr.GetScsiID() ?? Messages.UNKNOWN);

                // if in folder-view or if looking at SR on storagelink then display
                // location here
                if (Program.MainWindow.SelectionManager.Selection.HostAncestor == null && Program.MainWindow.SelectionManager.Selection.PoolAncestor == null)
                {
                    IXenObject belongsTo = Helpers.GetPool(sr.Connection);

                    if (belongsTo != null)
                    {
                        s.AddEntry(Messages.POOL, Helpers.GetName(belongsTo));
                    }
                    else
                    {
                        belongsTo = Helpers.GetMaster(sr.Connection);

                        if (belongsTo != null)
                        {
                            s.AddEntry(Messages.SERVER, Helpers.GetName(belongsTo));
                        }
                    }
                }
            }
            else if (xenObject is XenObject<Pool>)
            {
                Pool p = xenObject as Pool;
                if (p != null)
                {
                    s.AddEntry(Messages.POOL_LICENSE, p.LicenseString);
                    if (Helpers.ClearwaterOrGreater(p.Connection))
                        s.AddEntry(Messages.NUMBER_OF_SOCKETS, p.CpuSockets.ToString());
                }
            }
            else if (xenObject is StorageLinkPool)
            {
                var pool = (StorageLinkPool)xenObject;

                string capacityText = Util.DiskSizeString(pool.Capacity * 1024L * 1024L);
                string usedSpaceText = Util.DiskSizeString(pool.UsedSpace * 1024L * 1024L);
                string text = string.Format(Messages.STORAGELINK_POOL_SIZE_USED, usedSpaceText, capacityText);

                s.AddEntry(Messages.SIZE, text);
            }
            else if (xenObject is StorageLinkServer)
            {
                StorageLinkServer server = (StorageLinkServer)xenObject;
                s.AddEntry(Messages.USERNAME, server.StorageLinkConnection.Username);

                string error = server.StorageLinkConnection.Error;

                if (!string.IsNullOrEmpty(error))
                {
                    s.AddEntry(Messages.ERROR, error);
                }
            }
            else if (xenObject is StorageLinkSystem)
            {
                StorageLinkSystem sys = (StorageLinkSystem)xenObject;
                s.AddEntry(Messages.FULL_NAME, sys.FullName);
                s.AddEntry(Messages.MODEL, sys.Model);
                s.AddEntry(Messages.SERIAL_NUMBER, sys.SerialNumber);
            }

            s.AddEntry(FriendlyName("host.uuid"), GetUUID(xenObject));
        }

        private void generateStorageLinkBox()
        {
            SR sr = xenObject as SR;
            StorageLinkRepository slr = sr == null ? null : sr.StorageLinkRepository(Program.StorageLinkConnections);

            if (slr != null)
            {
                pdStorageLink.AddEntry(Messages.STORAGELINKSERVER, slr.StorageLinkConnection.Host);

                StorageLinkSystem system = slr.StorageLinkSystem;
                StorageLinkPool storagePool = slr.StorageLinkPool;

                if (system != null)
                {
                    pdStorageLink.AddEntry(Messages.STORAGE_SYSTEM, system.FriendlyName);
                }

                if (storagePool != null)
                {
                    pdStorageLink.AddEntry(Messages.STORAGE_POOL, storagePool.StorageLinkPoolPath);
                }
                pdStorageLink.AddEntry(Messages.RAID_TYPE, StorageLinkEnums.GetDisplayText<StorageLinkEnums.RaidType>(slr.RaidType));
                pdStorageLink.AddEntry(Messages.PROVISIONING_TYPE, StorageLinkEnums.GetDisplayText<StorageLinkEnums.ProvisioningType>(slr.ProvisioningType));
                pdStorageLink.AddEntry(Messages.PROVISIONING_OPTIONS, StorageLinkEnums.GetDisplayText<StorageLinkEnums.ProvisioningOptions>(slr.ProvisioningOptions));
            }
        }

        private void generateStorageLinkSystemCapabilitiesBox()
        {
            StorageLinkSystem system = xenObject as StorageLinkSystem;

            if (system != null)
            {
                var capabilities = new Dictionary<StorageLinkEnums.StorageSystemCapabilities, string>();

                capabilities.Add(StorageLinkEnums.StorageSystemCapabilities.ISCSI,
                    StorageLinkEnums.GetDisplayText<StorageLinkEnums.StorageSystemCapabilities>(StorageLinkEnums.StorageSystemCapabilities.ISCSI));
                capabilities.Add(StorageLinkEnums.StorageSystemCapabilities.FIBRE_CHANNEL,
                    StorageLinkEnums.GetDisplayText<StorageLinkEnums.StorageSystemCapabilities>(StorageLinkEnums.StorageSystemCapabilities.FIBRE_CHANNEL));
                capabilities.Add(StorageLinkEnums.StorageSystemCapabilities.PROVISION_FULL,
                    StorageLinkEnums.GetDisplayText<StorageLinkEnums.StorageSystemCapabilities>(StorageLinkEnums.StorageSystemCapabilities.PROVISION_FULL));
                capabilities.Add(StorageLinkEnums.StorageSystemCapabilities.PROVISION_THIN,
                    StorageLinkEnums.GetDisplayText<StorageLinkEnums.StorageSystemCapabilities>(StorageLinkEnums.StorageSystemCapabilities.PROVISION_THIN));
                capabilities.Add(StorageLinkEnums.StorageSystemCapabilities.POOL_LEVEL_DEDUPLICATION | StorageLinkEnums.StorageSystemCapabilities.VOLUME_LEVEL_DEDUPLICATION,
                    Messages.NEWSR_CSLG_DEDUPLICATION);
                capabilities.Add(StorageLinkEnums.StorageSystemCapabilities.DIFF_SNAPSHOT,
                    StorageLinkEnums.GetDisplayText<StorageLinkEnums.StorageSystemCapabilities>(StorageLinkEnums.StorageSystemCapabilities.DIFF_SNAPSHOT));
                capabilities.Add(StorageLinkEnums.StorageSystemCapabilities.REMOTE_REPLICATION,
                    StorageLinkEnums.GetDisplayText<StorageLinkEnums.StorageSystemCapabilities>(StorageLinkEnums.StorageSystemCapabilities.REMOTE_REPLICATION));
                capabilities.Add(StorageLinkEnums.StorageSystemCapabilities.CLONE,
                    StorageLinkEnums.GetDisplayText<StorageLinkEnums.StorageSystemCapabilities>(StorageLinkEnums.StorageSystemCapabilities.CLONE));
                capabilities.Add(StorageLinkEnums.StorageSystemCapabilities.RESIZE,
                    StorageLinkEnums.GetDisplayText<StorageLinkEnums.StorageSystemCapabilities>(StorageLinkEnums.StorageSystemCapabilities.RESIZE));
                capabilities.Add(StorageLinkEnums.StorageSystemCapabilities.CLONE_OF_SNAPSHOT,
                    StorageLinkEnums.GetDisplayText<StorageLinkEnums.StorageSystemCapabilities>(StorageLinkEnums.StorageSystemCapabilities.CLONE_OF_SNAPSHOT));
                capabilities.Add(StorageLinkEnums.StorageSystemCapabilities.SNAPSHOT_OF_SNAPSHOT,
                    StorageLinkEnums.GetDisplayText<StorageLinkEnums.StorageSystemCapabilities>(StorageLinkEnums.StorageSystemCapabilities.SNAPSHOT_OF_SNAPSHOT));

                foreach (StorageLinkEnums.StorageSystemCapabilities capability in capabilities.Keys)
                {
                    pdSectionStorageLinkSystemCapabilities.AddEntry(capabilities[capability], ((system.Capabilities & capability) != 0) ? Messages.YES : Messages.NO);
                }
            }
        }

        private static bool VMCanChooseHomeServer(VM vm)
        {
            if (vm != null && !vm.is_a_template)
            {
                String ChangeHomeReason = vm.IsOnSharedStorage();

                return !Helpers.WlbEnabledAndConfigured(vm.Connection) &&
                    (String.IsNullOrEmpty(ChangeHomeReason) || vm.HasNoDisksAndNoLocalCD);
            }
            return false;
        }


        private void GenTagRow(PDSection s)
        {
            List<ToolStripMenuItem> toolStrip = new List<ToolStripMenuItem>(new ToolStripMenuItem[] { EditMenuItem("GeneralEditPage", "") });

            string[] tags = Tags.GetTags(xenObject);
            if (tags != null && tags.Length > 0)
            {
                ToolStripMenuItem goToTag = MainWindow.NewToolStripMenuItem(Messages.VIEW_TAG_MENU_OPTION);

                foreach (string tag in tags)
                {
                    goToTag.DropDownItems.Add(MainWindow.NewToolStripMenuItem(tag.Ellipsise(30),
                        delegate(object sender, EventArgs e)
                        {
                            Program.MainWindow.SearchForTag(tag);
                        }));
                }
                toolStrip.Insert(0, goToTag);
                s.AddEntry(Messages.TAGS, TagsString(), toolStrip);
                return;
            }
            s.AddEntry(Messages.TAGS, Messages.NONE, toolStrip);
        }

        private string TagsString()
        {
            string[] tags = Tags.GetTags(xenObject);
            if (tags == null || tags.Length == 0)
                return Messages.NONE;

            List<string> tagsList = new List<string>(tags);
            tagsList.Sort();
            return string.Join(", ", tagsList.ToArray());
        }

        private void GenFolderRow(PDSection s)
        {
            List<ToolStripMenuItem> menuItems = new List<ToolStripMenuItem>();
            if (xenObject.Path != "")
            {
                menuItems.Add(
                    MainWindow.NewToolStripMenuItem(Messages.VIEW_FOLDER_MENU_OPTION,
                        delegate(object sender, EventArgs e)
                        {
                            Program.MainWindow.SearchForFolder(xenObject.Path);
                        })
                    );
            }
            menuItems.Add(EditMenuItem("GeneralEditPage", ""));
            s.AddEntry(
                Messages.FOLDER,
                new FolderListItem(xenObject.Path, FolderListItem.AllowSearch.None, false),
                menuItems
                );
        }

        private void generateMemoryBox()
        {
            Host host = xenObject as Host;
            if (host == null)
                return;

            PDSection s = pdSectionMemory;

      
            s.AddEntry(FriendlyName("host.ServerMemory"), host.HostMemoryString);
            s.AddEntry(FriendlyName("host.VMMemory"), host.ResidentVMMemoryUsageString);
            s.AddEntry(FriendlyName("host.XenMemory"), host.XenMemoryString);
            
        }

        private bool CPUsIdentical(IEnumerable<Host_cpu> cpus)
        {
            String cpuText = null;
            foreach (Host_cpu cpu in cpus)
            {
                if (cpuText == null)
                {
                    cpuText = Helpers.GetCPUProperties(cpu);
                    continue;
                }
                if (Helpers.GetCPUProperties(cpu) != cpuText)
                    return false;
            }
            return true;
        }

        private string hostAppliedPatches(Host host)
        {
            List<string> result = new List<string>();

            foreach (Pool_patch patch in host.AppliedPatches())
                result.Add(patch.Name);

            result.Sort(StringUtility.NaturalCompare);

            return string.Join("\n", result.ToArray());
        }

        private string hostUnappliedPatches(Host host)
        {
            List<string> result = new List<string>();

            foreach (Pool_patch patch in Pool_patch.GetAllThatApply(host, ConnectionsManager.XenConnectionsCopy))
            {
                if (!patch.AppliedTo(ConnectionsManager.XenConnectionsCopy).Contains(new XenRef<Host>(xenObject.opaque_ref)))
                    result.Add(patch.Name);
            }

            result.Sort(StringUtility.NaturalCompare);
            return string.Join("\n", result.ToArray());
        }

        #region VM delegates

        private static string HVMBootOrder(VM vm)
        {
            var order = vm.BootOrder.ToUpper().Union(new[] { 'D', 'C', 'N' });
            return string.Join("\n", order.Select(c => new BootDevice(c).ToString()).ToArray());
        }

        #endregion

        #region Pool delegates

        private string poolAppliedPatches()
        {
            return poolPatchString(patch => patch.host_patches.Count == xenObject.Connection.Cache.HostCount);
        }

        private string poolPartialPatches()
        {
            return poolPatchString(patch => patch.host_patches.Count > 0 &&
                                            patch.host_patches.Count != xenObject.Connection.Cache.HostCount);
        }

        private string poolNotAppliedPatches()
        {
            return poolPatchString(patch => patch.host_patches.Count == 0);
        }

        private string poolPatchString(Predicate<Pool_patch> predicate)
        {
            Pool_patch[] patches = xenObject.Connection.Cache.Pool_patches;

            List<String> output = new List<String>();

            foreach (Pool_patch patch in patches)
                if (predicate(patch))
                    output.Add(patch.name_label);

            output.Sort(StringUtility.NaturalCompare);

            return String.Join(",", output.ToArray());
        }

        #endregion

        private ToolStripMenuItem EditMenuItem(string tabname, string controlname)
        {
            return new CommandToolStripMenuItem(new PropertiesCommand(Program.MainWindow.CommandInterface, xenObject, tabname, controlname), Messages.EDIT, Properties.Resources.edit_16);
        }


        /// <summary>
        /// Checks for reboot warnings on all hosts in the pool and returns them as a list
        /// </summary>
        /// <param name="pool"></param>
        /// <returns></returns>
        private List<KeyValuePair<String, String>> CheckPoolUpdate(Pool pool)
        {
            List<KeyValuePair<String, String>> warnings = new List<KeyValuePair<string, string>>();
            foreach (Host host in xenObject.Connection.Cache.Hosts)
            {
                warnings.AddRange(CheckServerUpdates(host));
            }
            return warnings;
        }

        /// <summary>
        /// Checks the server has been restarted after any patches that require a restart were applied and returns a list of reboot warnings
        /// </summary>
        /// <param name="host"></param>
        /// <returns></returns>
        private List<KeyValuePair<String, String>> CheckServerUpdates(Host host)
        {
            List<Pool_patch> patches = host.AppliedPatches();
            List<KeyValuePair<String, String>> warnings = new List<KeyValuePair<String, String>>();
            double bootTime = host.BootTime;
            double agentStart = host.AgentStartTime;

            if (bootTime == 0.0 || agentStart == 0.0)
                return warnings;

            foreach (Pool_patch patch in patches)
            {
                double applyTime = Util.ToUnixTime(patch.AppliedOn(host));

                if (patch.after_apply_guidance.Contains(after_apply_guidance.restartHost)
                    && applyTime > bootTime)
                {
                    //TODO: Could we come up with a better key string than foopatch on blahhost? Also needs i18
                    warnings.Add(new KeyValuePair<String, String>(
                        String.Format("{0} on {1}", patch.Name, host.Name),
                        String.Format(Messages.GENERAL_PANEL_UPDATE_WARNING, host.Name, patch.Name)));
                }
                else if (patch.after_apply_guidance.Contains(after_apply_guidance.restartXAPI)
                    && applyTime > agentStart)
                {
                    // Actually, it only needs xapi restart, but we have no UI to do that.
                    warnings.Add(new KeyValuePair<String, String>(
                        String.Format("{0} on {1}", patch.Name, host.Name),
                        String.Format(Messages.GENERAL_PANEL_UPDATE_WARNING, host.Name, patch.Name)));
                }
            }
            return warnings;
        }

        private static string GetUUID(IXenObject o)
        {
            return o.Get("uuid") as String;
        }

        private static string FriendlyName(string propertyName)
        {
            return Core.PropertyManager.GetFriendlyName(string.Format("Label-{0}", propertyName)) ?? propertyName;
        }

        private void linkLabelExpand_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            foreach (PDSection s in sections)
            {
                if (!s.Parent.Visible)
                    continue;

                s.DisableFocusEvent = true;
                s.Expand();
                s.DisableFocusEvent = false;
            }

            linkLabelCollapse.Focus();
        }

        private void linkLabelCollapse_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            
            foreach (PDSection s in sections)
            {
                if (!s.Parent.Visible)
                    continue;

                s.DisableFocusEvent = true;
                s.Contract();
                s.DisableFocusEvent = false;
            }

            linkLabelExpand.Focus();
        }

        private void SetStatesOfExpandingLinks()
        {
            List<PDSection> sectionsVisible = sections.Where(section => section.Parent.Visible).ToList();
            bool anyExpanded = sectionsVisible.Any(s => s.IsExpanded);
            bool anyCollapsed = sectionsVisible.Any(s => !s.IsExpanded);
            linkLabelExpand.Enabled = anyCollapsed;
            linkLabelCollapse.Enabled = anyExpanded;
        }
    }
}
