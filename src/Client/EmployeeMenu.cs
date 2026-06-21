using ProjectResourceManagement.Client.Screens.Employee;

using ProjectResourceManagement.Client.Ui;

using ProjectResourceManagement.Shared.DTOs.Auth;

using ProjectResourceManagement.Shared.DTOs.Timesheet;



namespace ProjectResourceManagement.Client;



/// <summary>

/// Top-level Employee menu with missing-timesheet reminder banner before each display.

/// </summary>

internal static class EmployeeMenu

{

    public static async Task RunAsync(HttpClient client, LoginResponse session)

    {

        while (true)

        {

            ConsoleScreen.ShowHeader("Employee Menu", $"Welcome, {session.FullName}");

            await ShowMissingTimesheetBannerAsync(client);



            Console.WriteLine(" 1. Submit Timesheet");

            Console.WriteLine(" 2. View My Timesheets");

            Console.WriteLine(" 3. View My Allocations");

            Console.WriteLine(" 0. Logout");

            Console.Write("Enter option: ");



            switch (Console.ReadLine()?.Trim())

            {

                case "1":

                    await EmployeeTimesheetScreen.RunSubmitAsync(client, session);

                    break;

                case "2":

                    await EmployeeTimesheetScreen.RunHistoryAsync(client);

                    break;

                case "3":

                    await ShowAllocationsAsync(client);

                    break;

                case "0":

                    return;

                default:

                    ConsoleScreen.ShowError("Invalid option.");

                    ConsoleScreen.Pause();

                    break;

            }

        }

    }



    private static async Task ShowMissingTimesheetBannerAsync(HttpClient client)

    {

        var response = await client.GetAsync("/api/employee/timesheets/missing-reminder");

        if (!await ApiHelper.EnsureSuccessAsync(response))

        {

            return;

        }



        var reminder = await ApiHelper.ReadAsync<EmployeeTimesheetReminderDto>(response);

        if (reminder is { IsTimesheetSubmissionFrozen: true })
        {
            ConsoleScreen.ShowError(reminder.Message ?? "Timesheet submission is frozen. Contact your manager.");
            Console.WriteLine(new string('─', 46));
            Console.WriteLine();
            return;
        }

        if (reminder is { HasMissingTimesheet: true, MissingWeekStartDate: not null })
        {
            ConsoleScreen.ShowWarning(
                reminder.Message ?? $"Reminder: Timesheet for week {reminder.MissingWeekStartDate:yyyy-MM-dd} has not been submitted.");
            Console.WriteLine(new string('─', 46));
            Console.WriteLine();
        }
    }



    private static async Task ShowAllocationsAsync(HttpClient client)

    {

        ConsoleScreen.ShowHeader("My Allocations");



        var response = await client.GetAsync("/api/employee/allocations");

        if (!await ApiHelper.EnsureSuccessAsync(response))

        {

            ConsoleScreen.EndScreen();

            return;

        }



        var allocations = await ApiHelper.ReadAsync<List<EmployeeAllocationDto>>(response) ?? [];

        ConsoleTable.Print(

            ["Alloc ID", "Project", "Util %", "From", "To", "Status"],

            allocations.Select(item => new[]

            {

                item.AllocationId.ToString(),

                item.ProjectName,

                item.UtilizationPercentage.ToString("0.##"),

                item.FromDate.ToString("yyyy-MM-dd"),

                item.ToDate?.ToString("yyyy-MM-dd") ?? "-",

                item.Status.ToString()

            }));



        ConsoleScreen.EndScreen();

    }

}


