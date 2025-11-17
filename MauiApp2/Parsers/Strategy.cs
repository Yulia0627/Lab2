using MauiApp2.ViewModels;
using Microsoft.Maui.Controls;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;
using static System.Net.Mime.MediaTypeNames;

namespace MauiApp2.Parsers
{
    public class Teacher
    {
        public string Name { get; set; }
        public string Faculty { get; set; }
        public string Department { get; set; }
        public string Email { get; set; }
        public string Position { get; set; }

        public StudyPlan StudyPlan { get; set; }
        public StudentGroup StudentGroup { get; set; }

    }

    public class StudyPlan
    {
        public List<Subject> subjects = new();
    }

    public class Subject
    {
        public string SubjectName { get; set; }
        public int Hours { get; set; }

        public List<string> auditorium = new();
    }
    public class StudentGroup
    {
        public string GroupName { get; set; }
        public List<string> students = new();

    }
    public interface IParser
    {
        public string selectedValue1 { get; set; }//Ім'я
        public string selectedValue2 { get; set; }//Факультет
        public string selectedValue3 { get; set; }//Кафедра
        public string selectedValue4 { get; set; }//Посада
        public string selectedValue5 { get; set; }//Пошта
        public ObservableCollection<Teacher> Parse(string xmlPath);
        public void searchResult(string selectedValue1, string selectedValue2, string selectedValue3, string selectedValue4, string selectedValue5);
    }

    class SaxParser : IParser
    {
        public string selectedValue1 { get; set; }//Ім'я
        public string selectedValue2 { get; set; }//Факультет
        public string selectedValue3 { get; set; }//Кафедра
        public string selectedValue4 { get; set; }//Посада
        public string selectedValue5 { get; set; }//Пошта

        private readonly MainViewModel _vm;
        private string xmlPath;
        private XmlTextReader xmlReader;
        StringBuilder sb = new StringBuilder();
        public SaxParser(MainViewModel vm)
        {
            _vm = vm;
            selectedValue1 = _vm.SelectedValue1;
            selectedValue2 = _vm.SelectedValue2;
            selectedValue3 = _vm.SelectedValue3;
            selectedValue4 = _vm.SelectedValue4;
            selectedValue5 = _vm.SelectedValue5;
        }

        ObservableCollection<Teacher> teachers = new ObservableCollection<Teacher>();
        public ObservableCollection<Teacher> Parse(string xmlPath)
        {
            Teacher currentTeacher = null;
            Subject currentSubject = null;
            StudentGroup currentGroup = null;
            this.xmlPath = xmlPath;
            xmlReader = new XmlTextReader(xmlPath);
            {
                while (xmlReader.Read())
                {
                    if (xmlReader.NodeType == XmlNodeType.Element)
                    {
                        switch (xmlReader.Name)
                        {
                            case "teacher":
                                currentTeacher = new Teacher
                                {
                                    Name = xmlReader.GetAttribute("name"),
                                    Faculty = xmlReader.GetAttribute("faculty"),
                                    Department = xmlReader.GetAttribute("department"),
                                    Email = xmlReader.GetAttribute("email"),
                                    Position = xmlReader.GetAttribute("position"),
                                    StudyPlan = new StudyPlan(),
                                    StudentGroup = new StudentGroup()
                                };
                                break;

                            case "subject":
                                currentSubject = new Subject
                                {
                                    SubjectName = xmlReader.GetAttribute("name"),
                                    Hours = int.Parse(xmlReader.GetAttribute("hours"))
                                };
                                break;

                            case "auditorium":
                                xmlReader.Read();
                                if (xmlReader.NodeType == XmlNodeType.Text)
                                    currentSubject.auditorium.Add(xmlReader.Value.Trim('"'));
                                break;

                            case "studentGroup":
                                currentGroup = new StudentGroup
                                {
                                    GroupName = xmlReader.GetAttribute("name")
                                };
                                break;

                            case "student":
                                xmlReader.Read();
                                if (xmlReader.NodeType == XmlNodeType.Text)
                                    currentGroup.students.Add(xmlReader.Value.Trim('"'));
                                break;
                        }
                    }
                    else if (xmlReader.NodeType == XmlNodeType.EndElement)
                    {
                        switch (xmlReader.Name)
                        {
                            case "subject":
                                currentTeacher.StudyPlan.subjects.Add(currentSubject);
                                currentSubject = null;
                                break;

                            case "studentGroup":
                                currentTeacher.StudentGroup = currentGroup;
                                currentGroup = null;
                                break;

                            case "teacher":
                                teachers.Add(currentTeacher);
                                currentTeacher = null;
                                break;
                        }
                    }
                }
            }

            return teachers;
        }


        public void searchResult(string selectedValue1, string selectedValue2, string selectedValue3, string selectedValue4, string selectedValue5)
        {
            sb.Clear();
            bool found = false;

            var selectedValues = new List<string> { selectedValue1, selectedValue2, selectedValue3, selectedValue4, selectedValue5 };

            xmlReader = new XmlTextReader(xmlPath);
            StringBuilder currentNodeText = new StringBuilder();
            Dictionary<string, string> currentAttributes = new Dictionary<string, string>();

            string[] keys = { "name", "faculty", "department", "position", "email" };

            while (xmlReader.Read())
            {
                if (xmlReader.NodeType == XmlNodeType.Element &&
                    xmlReader.Name == "teacher")
                {
                    currentAttributes.Clear();

                    if (xmlReader.HasAttributes)
                    {
                        while (xmlReader.MoveToNextAttribute())
                            currentAttributes[xmlReader.Name] = xmlReader.Value;

                        xmlReader.MoveToElement();
                    }

                    bool matches = true;

                    for (int i = 0; i < selectedValues.Count; i++)
                    {
                        if (!string.IsNullOrEmpty(selectedValues[i]))
                        {
                            if (!currentAttributes.ContainsKey(keys[i]) ||
                                currentAttributes[keys[i]] != selectedValues[i])
                            {
                                matches = false;
                                break;
                            }
                        }
                    }

                    if (!matches)
                        continue;
                    found = true;

                    getInfo(xmlReader, currentAttributes);

                    _vm.TextToView = sb.ToString();

                    return;
                }
            }
            if (!found)
            {
                Shell.Current.DisplayAlert("Filter", "No teachers match the selected criteria.", "OK");
            }
        }

        void getInfo(XmlReader reader, Dictionary<string, string> currentAttributes)
        {
            sb.AppendLine($"{reader.Name}:");

            foreach (var attr in currentAttributes)
            {
                sb.AppendLine($"{attr.Key}: {attr.Value}");
            }

            if (!reader.IsEmptyElement)
            {
                int currentDepth = reader.Depth;

                while (reader.Read() && reader.Depth > currentDepth)
                {
                    if (reader.NodeType == XmlNodeType.Element)
                    {
                        var subAttrs = new Dictionary<string, string>();
                        if (reader.HasAttributes)
                        {
                            while (reader.MoveToNextAttribute())
                                subAttrs[reader.Name] = reader.Value;

                            reader.MoveToElement();
                        }

                        if (reader.IsEmptyElement)
                        {
                            sb.Append($"{reader.Name}: (empty)");
                        }
                        else
                        {
                            getInfo(reader, subAttrs);
                        }
                    }
                    else if (reader.NodeType == XmlNodeType.Text)
                    {
                        string value = reader.Value.Trim();
                        if (!string.IsNullOrEmpty(value))
                            sb.Append($" {value}\n");
                    }
                }
            }
        }
    }



    class DomParser : IParser
    {
        public string selectedValue1 { get; set; }//Ім'я
        public string selectedValue2 { get; set; }//Факультет
        public string selectedValue3 { get; set; }//Кафедра
        public string selectedValue4 { get; set; }//Посада
        public string selectedValue5 { get; set; }//Пошта

        private readonly MainViewModel _vm;
        private XmlDocument xmlDoc = new XmlDocument();
        public DomParser(MainViewModel vm)
        {
            _vm = vm;
            selectedValue1 = _vm.SelectedValue1;
            selectedValue2 = _vm.SelectedValue2;
            selectedValue3 = _vm.SelectedValue3;
            selectedValue4 = _vm.SelectedValue4;
            selectedValue5 = _vm.SelectedValue5;
        }
        public ObservableCollection<Teacher> Parse(string xmlPath)
        {
            ObservableCollection<Teacher> teachers = new ObservableCollection<Teacher>();

            xmlDoc.Load(xmlPath);

            XmlNodeList teacherNodes = xmlDoc.GetElementsByTagName("teacher");

            foreach (XmlNode teacherNode in teacherNodes)
            {
                Teacher currentTeacher = new Teacher
                {
                    Name = teacherNode.Attributes["name"]?.Value,
                    Faculty = teacherNode.Attributes["faculty"]?.Value,
                    Department = teacherNode.Attributes["department"]?.Value,
                    Email = teacherNode.Attributes["email"]?.Value,
                    Position = teacherNode.Attributes["position"]?.Value,
                    StudyPlan = new StudyPlan(),
                    StudentGroup = new StudentGroup()
                };

                XmlNode studyPlanNode = teacherNode.SelectSingleNode("studyPlan");
                if (studyPlanNode != null)
                {
                    XmlNodeList subjectNodes = studyPlanNode.SelectNodes("subject");
                    foreach (XmlNode subjectNode in subjectNodes)
                    {
                        Subject subject = new Subject
                        {
                            SubjectName = subjectNode.Attributes["name"]?.Value,
                            Hours = int.Parse(subjectNode.Attributes["hours"]?.Value ?? "0")
                        };

                        XmlNodeList auditoriumNodes = subjectNode.SelectNodes("auditorium");
                        foreach (XmlNode audNode in auditoriumNodes)
                        {
                            subject.auditorium.Add(audNode.InnerText.Trim());
                        }

                        currentTeacher.StudyPlan.subjects.Add(subject);
                    }
                }

                XmlNode studentGroupNode = teacherNode.SelectSingleNode("studentGroup");
                if (studentGroupNode != null)
                {
                    currentTeacher.StudentGroup.GroupName = studentGroupNode.Attributes["name"]?.Value;
                    XmlNodeList studentNodes = studentGroupNode.SelectNodes("student");
                    foreach (XmlNode studentNode in studentNodes)
                    {
                        currentTeacher.StudentGroup.students.Add(studentNode.InnerText.Trim());
                    }
                }
                teachers.Add(currentTeacher);
            }
            return teachers;
        }

        public void searchResult(string selectedValue1, string selectedValue2, string selectedValue3, string selectedValue4, string selectedValue5)
        {
            List<string> conditions = new List<string>();
            if (!string.IsNullOrEmpty(selectedValue1))
            {
                conditions.Add($"@name = '{selectedValue1}'");
            }
            if (!string.IsNullOrEmpty(selectedValue2))

            {
                conditions.Add($"@faculty = '{selectedValue2}'");
            }
            if (!string.IsNullOrEmpty(selectedValue3))
            {
                conditions.Add($"@department = '{selectedValue3}'");
            }
            if (!string.IsNullOrEmpty(selectedValue4))
            {
                conditions.Add($"@position = '{selectedValue4}'");
            }
            if (!string.IsNullOrEmpty(selectedValue5))
            {
                conditions.Add($"@email = '{selectedValue5}'");
            }
            string xpath;
            if (conditions.Count > 0)
            {
                xpath = "//teacher[" + string.Join(" and ", conditions) + "]";
            }
            else
            {
                xpath = "//teacher";
            }
            XmlNodeList filteredTeachers = xmlDoc.SelectNodes(xpath);
            StringBuilder sb = new StringBuilder();

            if (filteredTeachers.Count == 0)
            {
                Shell.Current.DisplayAlert("Filter", "No attributes match the selected criteria.", "OK");
            }
            else
            {
                foreach (XmlNode el in filteredTeachers)
                {
                    getInfo(el, sb);
                }

                _vm.TextToView = sb.ToString();
            }
        }

        void getInfo(XmlNode element, StringBuilder sb)
        {
            if (element.Name == "#text")
                return;

            sb.Append($"\n{element.Name}:");

            if (element.Attributes != null && element.Attributes.Count > 0)
            {
                foreach (XmlAttribute attr in element.Attributes)
                {
                    sb.Append($"\n{attr.Name}: {attr.Value}");
                }
            }

            if (element.HasChildNodes)
            {
                foreach (XmlNode child in element.ChildNodes)
                {
                    if (child.NodeType == XmlNodeType.Text)
                    {
                        sb.Append($"\n{child.InnerText}");
                    }
                    else
                    {
                        getInfo(child, sb);
                    }
                }
            }
        }

    }

    class LinqToXmlParser : IParser
    {
        public string selectedValue1 { get; set; }//Ім'я
        public string selectedValue2 { get; set; }//Факультет
        public string selectedValue3 { get; set; }//Кафедра
        public string selectedValue4 { get; set; }//Посада
        public string selectedValue5 { get; set; }//Пошта
        private readonly MainViewModel _vm;
        private XDocument doc = new XDocument();
        public LinqToXmlParser(MainViewModel vm)
        {
            _vm = vm;
            selectedValue1 = _vm.SelectedValue1;
            selectedValue2 = _vm.SelectedValue2;
            selectedValue3 = _vm.SelectedValue3;
            selectedValue4 = _vm.SelectedValue4;
            selectedValue5 = _vm.SelectedValue5;
        }

        public ObservableCollection<Teacher> Parse(string xmlPath)
        {
            ObservableCollection<Teacher> teachers = new ObservableCollection<Teacher>();

            doc = XDocument.Load(xmlPath);

            var teacherNodes = doc.Descendants("teacher");

            foreach (var teacherNode in teacherNodes)
            {
                Teacher teacher = new Teacher
                {
                    Name = (string)teacherNode.Attribute("name"),
                    Faculty = (string)teacherNode.Attribute("faculty"),
                    Department = (string)teacherNode.Attribute("department"),
                    Email = (string)teacherNode.Attribute("email"),
                    Position = (string)teacherNode.Attribute("position"),
                    StudyPlan = new StudyPlan(),
                    StudentGroup = new StudentGroup()
                };

                var subjects = teacherNode
                    .Element("studyPlan")?
                    .Elements("subject");

                if (subjects != null)
                {
                    foreach (var subjectNode in subjects)
                    {
                        Subject subject = new Subject
                        {
                            SubjectName = (string)subjectNode.Attribute("name"),
                            Hours = (int?)subjectNode.Attribute("hours") ?? 0
                        };

                        subject.auditorium.AddRange(
                            subjectNode.Elements("auditorium").Select(a => a.Value.Trim())
                        );

                        teacher.StudyPlan.subjects.Add(subject);
                    }
                }

                var groupNode = teacherNode.Element("studentGroup");
                if (groupNode != null)
                {
                    teacher.StudentGroup.GroupName = (string)groupNode.Attribute("name");

                    teacher.StudentGroup.students.AddRange(
                        groupNode.Elements("student").Select(s => s.Value.Trim())
                    );
                }

                teachers.Add(teacher);
            }

            return teachers;
        }


        public void searchResult(string selectedValue1, string selectedValue2, string selectedValue3, string selectedValue4, string selectedValue5)
        {
            var filter = doc.Descendants("teacher");

            if (selectedValue1 != null)
            {
                filter = (from el in filter
                          where el.Attribute("name") != null &&
                               el.Attribute("name").Value == selectedValue1
                          select el);
            }

            if (selectedValue2 != null)
            {
                filter = (from el in filter
                          where el.Attribute("faculty") != null &&
                               el.Attribute("faculty").Value == selectedValue2
                          select el);
            }

            if (selectedValue3 != null)
            {
                filter = (from el in filter
                          where el.Attribute("department") != null &&
                               el.Attribute("department").Value == selectedValue3
                          select el);
            }

            if (selectedValue4 != null)
            {
                filter = (from el in filter
                          where el.Attribute("position") != null &&
                               el.Attribute("position").Value == selectedValue4
                          select el);
            }

            if (selectedValue5 != null)
            {
                filter = (from el in filter
                          where el.Attribute("email") != null &&
                               el.Attribute("email").Value == selectedValue5
                          select el);
            }

            StringBuilder sb = new StringBuilder();

            foreach (var el in filter)
            {
                getInfo(el, sb);
            }
            _vm.TextToView = sb.ToString();
            if (_vm.TextToView == null)
            {
                Shell.Current.DisplayAlert("Filter", "No attributes match the selected criteria.", "OK");
            }
        }

        void getInfo(XElement element, StringBuilder sb)
        {
            sb.AppendLine($"{element.Name}:");

            if (element.HasAttributes)
            {
                foreach (var attr in element.Attributes())
                {
                    sb.AppendLine($"{attr.Name}: {attr.Value}");
                }
            }

            if (!element.HasElements && !string.IsNullOrWhiteSpace(element.Value))
            {
                sb.AppendLine($"{element.Value.Trim()}");
            }

            foreach (var el in element.Elements())
            {
                getInfo(el, sb);
            }
        }
    }



    class Parser
    {
        private IParser _strategy;

        public Parser(IParser strategy)
        {
            _strategy = strategy;
        }

        public void setStrategy(IParser strategy)
        {
            _strategy = strategy;
        }

        public ObservableCollection<Teacher> doParse(string xmlPath)
        {
            return _strategy.Parse(xmlPath);
        }
        public void doSearchResult(string selectedValue1, string selectedValue2, string selectedValue3, string selectedValue4, string selectedValue5)
        {
            _strategy.searchResult(selectedValue1, selectedValue2, selectedValue3, selectedValue4, selectedValue5);
        }
    }
}




