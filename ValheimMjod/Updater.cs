﻿using Rollbar;
using Squirrel;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Windows;

namespace ValheimMjod
{
    public class Updater
    {
        private readonly AutoResetEvent _autoEvent;
        [System.Diagnostics.CodeAnalysis.SuppressMessage("CodeQuality", "IDE0052:Remove unread private members", Justification = "<Pending>")]
        private readonly Timer _timer;

        public Updater()
        {
#if DEBUG
            return;
#endif
            _autoEvent = new AutoResetEvent(false);
            _timer = new Timer(CheckStatus, _autoEvent, 5000, 30000);
        }

        private void CheckStatus(object stateInfo)
        {
            CheckUpdates();
        }

        public async void CheckUpdates()
        {
            try
            {
                using (var mgr = new UpdateManager(Settings.UpdatePath))
                {
                    SquirrelAwareApp.HandleEvents(
                        onInitialInstall: v =>
                        {
                            mgr.CreateShortcutForThisExe();
                        },
                        onAppUpdate: v =>
                        {
                            mgr.CreateShortcutForThisExe();
                        },
                        onAppUninstall: v =>
                        {
                            mgr.RemoveShortcutForThisExe();
                        },
                        onFirstRun: () =>
                        {
                        });

                    if (!mgr.IsInstalledApp) //not installed during Squirrel, skipping update
                        return;

                    var installedVersion = mgr.CurrentlyInstalledVersion();
                    var entry = await mgr.UpdateApp();
                    if (entry != null && (installedVersion == null || entry.Version > installedVersion))
                    {
                        if (MessageBox.Show("You need to restart app for apply changes.\r\n\r\nRestart now?", "Valheim Mjöð have been updated", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
                        {
                            try
                            {
                                var location = Application.ResourceAssembly.Location;
                                var filename = Path.GetFileName(location);
                                var fullPath = Path.GetFullPath(Path.Combine(Path.GetDirectoryName(location), "..", filename));
                                System.Diagnostics.Process.Start(fullPath);
                            }
                            catch (Exception e)
                            {
                                throw e;
                            }
                            finally
                            {
                                Application.Current.Dispatcher.Invoke(Application.Current.Shutdown);
                            }
                        }
                    }
                }
            }
            catch (Exception e)
            {
                RollbarLocator.RollbarInstance.Error(e, new Dictionary<string, object>()
                {
                    ["version"] = Settings.Version.ToString()
                });
            }
        }
    }
}
