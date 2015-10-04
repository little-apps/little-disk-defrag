using System;
using System.Security.Principal;

namespace Little_Disk_Defrag.Misc
{
    class Permissions
    {
        public static void SetPrivileges(bool Enabled)
        {
            SetPrivilege("SeBackupPrivilege", Enabled);
        }

        public static bool SetPrivilege(string privilege, bool enabled)
        {
            try
            {
                PInvoke.TokPriv1Luid tp = new PInvoke.TokPriv1Luid();
                IntPtr hproc = System.Diagnostics.Process.GetCurrentProcess().Handle;
                IntPtr htok = IntPtr.Zero;

                if (!PInvoke.OpenProcessToken(hproc, PInvoke.TOKEN_ADJUST_PRIVILEGES | PInvoke.TOKEN_QUERY, ref htok))
                    return false;

                if (!PInvoke.LookupPrivilegeValue(null, privilege, ref tp.Luid))
                    return false;

                tp.Count = 1;
                tp.Luid = 0;
                tp.Attr = ((enabled) ? (PInvoke.SE_PRIVILEGE_ENABLED) : (0));

                bool bRet = (PInvoke.AdjustTokenPrivileges(htok, false, ref tp, 0, IntPtr.Zero, IntPtr.Zero));

                // Cleanup
                PInvoke.CloseHandle(htok);

                return bRet;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Checks if the user is an admin
        /// </summary>
        /// <returns>True if it is in admin group</returns>
        public static bool IsUserAdministrator
        {
            get
            {
                //bool value to hold our return value
                bool isAdmin;
                try
                {
                    //get the currently logged in user
                    WindowsIdentity user = WindowsIdentity.GetCurrent();
                    WindowsPrincipal principal = new WindowsPrincipal(user);
                    isAdmin = principal.IsInRole(WindowsBuiltInRole.Administrator);
                }
                catch (UnauthorizedAccessException ex)
                {
                    isAdmin = false;
#if (DEBUG)
                    throw ex;
#endif
                }
                catch (Exception ex)
                {
                    isAdmin = false;
#if (DEBUG)
                    throw ex;
#endif
                }
                return isAdmin;
            }
        }
    }
}
