using Projekt.Models;
using System.Collections.ObjectModel;
using Projekt.Controls;
using System.Diagnostics;

namespace Projekt.Views;

public partial class MainPage : ContentPage
{
    readonly string classesFolderPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Classes");
    List<Student> Students;
    string CurrentClass;
    int LuckyNumber = -1;

    public MainPage()
    {
        InitializeComponent();
        Loaded += PageLoaded;
    }

    protected async void PageLoaded(object sender, EventArgs e)
    {
        string[] classFiles = LoadClassFiles(classesFolderPath);

        if (classFiles.Any())
        {
            string[] classNames = classFiles.Select(Path.GetFileNameWithoutExtension).ToArray();

            var selectedClassFile = await DisplayActionSheet("Wybierz klas�", "Anuluj", null, classNames);

            if (selectedClassFile != "Anuluj" && !string.IsNullOrEmpty(selectedClassFile))
            {
                var lines = File.ReadAllLines(Path.Combine(classesFolderPath, $"{selectedClassFile}.txt"));
                Students = new List<Student>();
                foreach (var line in lines)
                {
                    var parts = line.Split(',');
                    if (parts.Length >= 2)
                    {
                        Students.Add(new Student
                        {
                            StudentNumber = int.Parse(parts[0].Trim()),
                            Name = parts[1].Trim(),
                            IsPresent = parts[2].Trim() == "+"
                        });
                    }
                }

                CurrentClass = selectedClassFile;

                UpdateStudentsLayout();
            }

        }
        else
        {
            CreateNewClassFile("Nie znaleziono �adnej klasy!");
        }
    }

    private void CreateButton_Clicked(object sender, EventArgs e)
    {
        if (Directory.Exists(classesFolderPath))
        {
            CreateNewClassFile("Utw�rz now� klas�!");
        }
    }

    private async void AddStudentButton_Clicked(object sender, EventArgs e)
    {
        if (string.IsNullOrEmpty(CurrentClass))
        {
            await DisplayAlert("B��d!", "Nie wybrano �adnej klasy, wybierz klas� aby mo�na by�o doda� ucznia.", "Anuluj");
            return;
        }

        string studentName = await DisplayPromptAsync("Dodaj ucznia", "Podaj imi� ucznia", "Potwierd�", "Anuluj");

        if (!string.IsNullOrEmpty(studentName))
        {
            Student newStudent = new Student { Name = studentName, IsPresent = true };
            Students.Add(newStudent);

            UpdateJSON();
            UpdateStudentsLayout();
        }
    }

    private async void RandomStudentButton_Clicked(object sender, EventArgs e)
    {
        if (Students != null && Students.Any())
        {
            Random random = new Random();


            var filteredStudents = Students
                .Where(s => (LuckyNumber == -1 || s.StudentNumber != LuckyNumber) && s.IsPresent)
                .ToList();

            if (filteredStudents.Any())
            {
                int randomIndex = random.Next(0, filteredStudents.Count);
                var randomStudent = filteredStudents[randomIndex];

                

                await DisplayAlert("Wylosowany Ucze�", randomStudent.Name, "Potwierd�");
            }
            else
            {
                await DisplayAlert("B��d", "Nikt z klasy nie spe�nia wymaga�", "Potwierd� :(");
            }
        }
    }

    public void RemoveStudent(Student student)
    {
        int idx = Students.IndexOf(student);

        Students.Remove(student);
        Student.DecrementNextStudentNumber();

        if (Students.Count == 0)
        {
            UpdateJSON();
            UpdateStudentsLayout();
        }
        else
        {
            if (Students.Count > 1)
            {
                for (int i = idx; i < Students.Count; i++)
                {
                    Students[i].StudentNumber--;
                }
            }
            else
            {
                Students[0].StudentNumber--;
            }
            UpdateJSON();
            UpdateStudentsLayout();
        }
    }
    private void LuckyNumberButton_Clicked(object sender, EventArgs e)
    {
        if (CurrentClass == null)
            return;

        Random random = new Random();

        LuckyNumber = random.Next(1, Students.Count + 1);

        LuckyNumberLabel.Text = $"Szcz�liwy numerek: {LuckyNumber}";
    }

    public async void UpdateJSON()
    {
        if (string.IsNullOrEmpty(CurrentClass))
        {
            await DisplayAlert("B��d", "Nie wybrano �adnej klasy, wybierz klas� aby mo�na by�o j� zapisa�.", "Anuluj");
            return;
        }
        var studentLines = Students.Select(s => $"{s.StudentNumber},{s.Name},{(s.IsPresent ? "+" : "-")}");
        File.WriteAllLines(Path.Combine(classesFolderPath, $"{CurrentClass}.txt"), studentLines);
    }

    private void UpdateStudentsLayout()
    {
        stackLayout.Children.Clear();

        if (Students != null)
        {
            foreach (var student in Students)
            {
                StudentView studentView = new(this, student);
                stackLayout.Children.Add(studentView);
            }
        }
    }

    private async void CreateNewClassFile(string message)
    {
        string className = await DisplayPromptAsync(message, "Podaj nazw� nowej klasy", "Potwierd�", "Anuluj");

        if (!string.IsNullOrEmpty(className))
        {
            var newClassFilePath = Path.Combine(classesFolderPath, $"{className}.txt");
            File.WriteAllText(newClassFilePath, "");

            Students = new List<Student>();
            CurrentClass = className;
        }
    }

    private void DeleteClassButton_Clicked(object sender, EventArgs e)
    {
        try
        {
            if (string.IsNullOrEmpty(CurrentClass))
            {
                DisplayAlert("B��d", "�adna klasa nie jest aktualnie wybrana.", "Potwierd�");
                return;
            }

            var classFilePath = Path.Combine(classesFolderPath, $"{CurrentClass}.txt");
            string classToDelete = CurrentClass;

            if (File.Exists(classFilePath))
            {
                File.Delete(classFilePath);
                CurrentClass = null;
                Students = null;
                UpdateStudentsLayout();

                DisplayAlert("Sukces", $"Klasa '{classToDelete}' zosta�a pomy�lnie usuni�ta.", "Potwierd�");
            }
            else
            {
                DisplayAlert("B��d", $"Klasa '{classToDelete}' nie istnieje.", "Potwierd�");
            }
        }
        catch (Exception ex)
        {
            DisplayAlert("B��d", $"Wyst�pi� b��d: {ex.Message}", "Potwierd�");
        }
    }

    static private string[] LoadClassFiles(string classesFolderPath)
    {
        if (!Directory.Exists(classesFolderPath))
        {
            Directory.CreateDirectory(classesFolderPath);
        }
        string[] classFiles = Directory.GetFiles(classesFolderPath);

        classFiles = classFiles.Where(file => Path.GetExtension(file).ToLower() == ".txt").ToArray();

        return classFiles;
    }
}