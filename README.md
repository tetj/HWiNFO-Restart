# HWiNFO-Restart

A windowless background helper for `C:\Program Files\HWiNFO64\HWiNFO64.EXE`.

- **Main goal** : restarts the HWiNFO64 process every 11 hours
- Starts HWiNFO64 if it is not already running; does not immediately restart an existing instance.
- Terminates and relaunches HWiNFO64 after 11 hours, measured from a successful launch (or helper startup if HWiNFO64 was already running).
- Checks once per minute and automatically relaunches HWiNFO64 if it was closed. Each successful launch resets the 11-hour timer.
- Retries failed starts or restarts once per minute without dialogs, console windows, keyboard input, or elevation requests.
- Waits up to 30 seconds for each terminated process to exit before starting a replacement. A failed termination does not launch a duplicate.
- Allows only one helper instance per Windows session.

## Unattended startup

HWiNFO64 normally needs administrator privileges. Windows does not allow this helper to silently elevate itself. For silent operation, launch it with privileges already granted:

1. Build the solution in Release configuration and copy the output to a stable location.
2. Open Windows Task Scheduler and choose **Create Task** (not Create Basic Task).
3. Select your Windows account, **Run only when user is logged on**, and **Run with highest privileges**. Task creation may require a one-time administrator approval.
4. Add an **At log on** trigger for your account.
5. Add a **Start a program** action pointing to the rebuilt `HWiNFO-Restart.exe`.
6. Under Settings, disable **Stop the task if it runs longer than** and select **Do not start a new instance** if the task is already running. On a laptop, also disable power conditions that would prevent or stop the task on battery if continuous operation is desired.
7. Remove old startup entries or scheduled tasks for previous helper builds, stop the old helper, and run the new task.

Do not configure the task to repeat every 11 hours; the helper maintains its own timer. Keep it running. Restarts cannot take place while Windows is asleep or shut down.

The helper never requests elevation (`runas`). A non-elevated launch logs an actionable error and exits with code 1 before attempting to manage HWiNFO64; start the scheduled task instead of double-clicking the executable. Once elevated, the helper uses the Windows shell launcher because HWiNFO64's `requireAdministrator` / `uiAccess` manifest cannot be satisfied by a direct `CreateProcess` launch even from an elevated helper. The helper does not control HWiNFO64's own windows or dialogs; configure HWiNFO64's startup/minimize options separately.

## Diagnostics

Activity and failures are written to `%LOCALAPPDATA%\HWiNFO-Restart\restart.log` for the account running the helper. If this location cannot be written, the error and original message are sent to .NET diagnostic trace output instead. No error dialog is shown by the helper.
