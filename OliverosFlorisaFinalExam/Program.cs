using System;
using System.IO;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

class LoanRecord
{
    public string RecordId;
    public string StudentName;
    public string BookTitle;
    public string BorrowDate;
    public string ReturnDate;
    public DateTime CreatedAt;
    public DateTime UpdatedAt;
    public bool IsActive;
    public string Checksum;

    public override string ToString()
    {
        return RecordId + "|" +
               StudentName + "|" +
               BookTitle + "|" +
               BorrowDate + "|" +
               ReturnDate + "|" +
               CreatedAt + "|" +
               UpdatedAt + "|" +
               IsActive + "|" +
               Checksum;
    }

    public static LoanRecord FromString(string line)
    {
        string[] p = line.Split('|');

        LoanRecord r = new LoanRecord();
        r.RecordId = p[0];
        r.StudentName = p[1];
        r.BookTitle = p[2];
        r.BorrowDate = p[3];
        r.ReturnDate = p[4];
        r.CreatedAt = DateTime.Parse(p[5]);
        r.UpdatedAt = DateTime.Parse(p[6]);
        r.IsActive = bool.Parse(p[7]);
        r.Checksum = p[8];

        return r;
    }
}

class Program
{
    static string folder = "Data";
    static string recordsFile = "Data/loans.txt";
    static string auditFile = "Data/audit.txt";
    static string reportFile = "Data/report.txt";

    static void Main()
    {
        InitializeStorage();

        while (true)
        {
            Console.WriteLine("\n===== LIBRARY LOAN SYSTEM =====");
            Console.WriteLine("1. Add Loan");
            Console.WriteLine("2. View/Search Loans");
            Console.WriteLine("3. Update Loan");
            Console.WriteLine("4. Soft Delete");
            Console.WriteLine("5. Hard Delete");
            Console.WriteLine("6. Generate Report");
            Console.WriteLine("7. Exit");
            Console.Write("Choose: ");

            string choice = Console.ReadLine();

            try
            {
                if (choice == "1") AddLoan();
                else if (choice == "2") ViewLoans();
                else if (choice == "3") UpdateLoan();
                else if (choice == "4") SoftDelete();
                else if (choice == "5") HardDelete();
                else if (choice == "6") GenerateReport();
                else if (choice == "7") break;
                else Console.WriteLine("Invalid Choice");
            }
            catch (Exception ex)
            {
                Log("ERROR", ex.Message);
                Console.WriteLine("Error: " + ex.Message);
            }
        }
    }

    static void InitializeStorage()
    {
        if (!Directory.Exists(folder))
            Directory.CreateDirectory(folder);

        if (!File.Exists(recordsFile))
            File.Create(recordsFile).Close();

        if (!File.Exists(auditFile))
            File.Create(auditFile).Close();

        Log("SYSTEM", "Storage Initialized");
    }

    static List<LoanRecord> LoadRecords()
    {
        List<LoanRecord> records = new List<LoanRecord>();

        string[] lines = File.ReadAllLines(recordsFile);

        foreach (string line in lines)
        {
            if (line.Trim() != "")
            {
                try
                {
                    records.Add(LoanRecord.FromString(line));
                }
                catch
                {
                    Log("ERROR", "Malformed Record");
                }
            }
        }

        return records;
    }

    static void SaveRecords(List<LoanRecord> records)
    {
        List<string> lines = new List<string>();

        foreach (LoanRecord r in records)
            lines.Add(r.ToString());

        File.WriteAllLines(recordsFile, lines.ToArray());
    }

    static void AddLoan()
    {
        Console.Write("Student Name: ");
        string student = Console.ReadLine();

        Console.Write("Book Title: ");
        string book = Console.ReadLine();

        Console.Write("Borrow Date (YYYY-MM-DD): ");
        string borrow = Console.ReadLine();

        Console.Write("Return Date (YYYY-MM-DD): ");
        string ret = Console.ReadLine();

        if (!Regex.IsMatch(borrow, "^\\d{4}-\\d{2}-\\d{2}$") ||
            !Regex.IsMatch(ret, "^\\d{4}-\\d{2}-\\d{2}$"))
        {
            Console.WriteLine("Invalid Date Format");
            return;
        }

        List<LoanRecord> records = LoadRecords();

        LoanRecord r = new LoanRecord();

        Random rnd = new Random();
        r.RecordId = rnd.Next(100000000, 999999999).ToString();

        r.StudentName = student;
        r.BookTitle = book;
        r.BorrowDate = borrow;
        r.ReturnDate = ret;
        r.CreatedAt = DateTime.Now;
        r.UpdatedAt = DateTime.Now;
        r.IsActive = true;
        r.Checksum = GenerateChecksum(r);

        records.Add(r);

        SaveRecords(records);

        Log("ADD", r.RecordId);

        Console.WriteLine("Loan Added Successfully");
    }

    static void ViewLoans()
    {
        Console.Write("Search Book Title: ");
        string search = Console.ReadLine();

        List<LoanRecord> records = LoadRecords();

        foreach (LoanRecord r in records)
        {
            if (r.IsActive &&
                r.BookTitle.ToLower().Contains(search.ToLower()))
            {
                Console.WriteLine("----------------------");
                Console.WriteLine("ID: " + r.RecordId);
                Console.WriteLine("Student: " + r.StudentName);
                Console.WriteLine("Book: " + r.BookTitle);
                Console.WriteLine("Borrowed: " + r.BorrowDate);
                Console.WriteLine("Return: " + r.ReturnDate);
            }
        }

        Log("READ", search);
    }

    static void UpdateLoan()
    {
        List<LoanRecord> records = LoadRecords();

        Console.Write("Enter Record ID: ");
        string id = Console.ReadLine();

        foreach (LoanRecord r in records)
        {
            if (r.RecordId == id)
            {
                Console.Write("New Student Name: ");
                r.StudentName = Console.ReadLine();

                Console.Write("New Return Date: ");
                r.ReturnDate = Console.ReadLine();

                r.UpdatedAt = DateTime.Now;
                r.Checksum = GenerateChecksum(r);

                SaveRecords(records);

                Log("UPDATE", id);

                Console.WriteLine("Updated Successfully");
                return;
            }
        }

        Console.WriteLine("Record Not Found");
    }

    static void SoftDelete()
    {
        List<LoanRecord> records = LoadRecords();

        Console.Write("Enter Record ID: ");
        string id = Console.ReadLine();

        foreach (LoanRecord r in records)
        {
            if (r.RecordId == id)
            {
                r.IsActive = false;

                SaveRecords(records);

                Log("SOFT DELETE", id);

                Console.WriteLine("Soft Deleted");
                return;
            }
        }

        Console.WriteLine("Record Not Found");
    }

    static void HardDelete()
    {
        List<LoanRecord> records = LoadRecords();

        Console.Write("Enter Record ID: ");
        string id = Console.ReadLine();

        for (int i = 0; i < records.Count; i++)
        {
            if (records[i].RecordId == id)
            {
                records.RemoveAt(i);

                SaveRecords(records);

                Log("HARD DELETE", id);

                Console.WriteLine("Hard Deleted");
                return;
            }
        }

        Console.WriteLine("Record Not Found");
    }

    static void GenerateReport()
    {
        List<LoanRecord> records = LoadRecords();
        List<string> report = new List<string>();

        report.Add("ACTIVE LIBRARY LOANS");
        report.Add("----------------------");

        foreach (LoanRecord r in records)
        {
            if (r.IsActive)
            {
                report.Add(
                    r.RecordId + " | " +
                    r.StudentName + " | " +
                    r.BookTitle + " | Due: " +
                    r.ReturnDate
                );
            }
        }

        File.WriteAllLines(reportFile, report.ToArray());

        Log("REPORT", "Generated");

        Console.WriteLine("Report Generated");
    }

    static void Log(string action, string details)
    {
        File.AppendAllText(
            auditFile,
            DateTime.Now + " | " +
            action + " | " +
            details + Environment.NewLine
        );
    }

    static string GenerateChecksum(LoanRecord r)
    {
        string raw =
            r.RecordId +
            r.StudentName +
            r.BookTitle +
            r.BorrowDate +
            r.ReturnDate;

        SHA256 sha = SHA256.Create();

        byte[] bytes =
            sha.ComputeHash(
                Encoding.UTF8.GetBytes(raw));

        return Convert.ToBase64String(bytes);
    }
}