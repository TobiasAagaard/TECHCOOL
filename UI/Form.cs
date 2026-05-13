using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Reflection;

namespace TECHCOOL.UI
{
    public class Form<T>
    {
        T record;
        Dictionary<string, Field> fields = new();
        int field_edit_index = 0;
        string current_field
        {
            get
            {
                int i = 0;
                string title = "";
                foreach (KeyValuePair<string, Field> kv in fields)
                {
                    if (i == field_edit_index)
                    {
                        title = kv.Key;
                        break;
                    }
                    i++;
                }
                return title;
            }
        }

        public void AddField(string title, Field field)
        {
            fields.Add(title, field);
        }

        public Form<T> TextBox(string title, string property)
        {
            fields.Add(title, new TextBox { Title = title, Property = property });
            return this;
        }
        public Form<T> IntBox(string title, string property)
        {
            fields.Add(title, new IntBox { Title = title, Property = property });
            return this;
        }
        public Form<T> DoubleBox(string title, string property)
        {
            fields.Add(title, new DoubleBox { Title = title, Property = property });
            return this;
        }
        public Form<T> DecimalBox(string title, string property)
        {
            fields.Add(title, new DecimalBox { Title = title, Property = property });
            return this;
        }
        public Form<T> SelectBox(string title, string property, Dictionary<string, object> options = null)
        {
            if (options == null) options = new();
            fields.Add(title, new SelectBox { Title = title, Property = property, Options = options });
            return this;
        }

        public Form<T> SearchBox(string title, string property, Func<string, List<(string, object)>> search)
        {
            fields.Add(title, new SearchBox { Title = title, Property = property, Search = search });
            return this;
        }
        
        public void AddOption(string field, string option, object value)
        {
            if (!fields.ContainsKey(field))
            {
                throw new Exception("There is no such field in this form called " + field);
            }
            if (!(fields[field] is SelectBox))
            {
                throw new Exception("Field " + field + " is not a SelectBox");
            }
            var f = (SelectBox)fields[field];
            f.Options.Add(option, value);
        }

        public bool Edit(T record)
        {
            this.record = record;
            bool recordChanged = false;
            //Copy values from record into fields

            foreach (KeyValuePair<string, Field> kv in fields)
            {
                var field = kv.Value;
                var prop = record.GetType().GetProperty(field.Property);
                if (prop == null)
                {
                    throw new($"Form cannot edit: There is no property named '{field.Property}' in class '{record.GetType()}");
                }
                var value = prop.GetValue(record);
                if (value != null)
                {
                    field.Value = value;
                }
            }

            int x, y;
            (x, y) = Console.GetCursorPosition();
            ConsoleKeyInfo key;
            do
            {
                Console.SetCursorPosition(x, y);
                field_edit_index = Math.Clamp(field_edit_index, 0, fields.Count - 1);
                Draw();
                key = Console.ReadKey(true);
                try {
                switch (key.Key)
                {
                    case ConsoleKey.Enter:
                        fields[current_field].Enter();

                        string property_name = fields[current_field].Property;
                        object property_value = fields[current_field].Value;

                        PropertyInfo property = record.GetType().GetProperty(property_name);
                        if (property == null)
                        {
                            Console.WriteLine($"Form: No such property '{property_name}' on {record.GetType()}");
                            return false;
                        }

                        if (property.GetValue(record) != property_value)
                        {
                            property.SetValue(record, property_value);
                            recordChanged = true;
                        }

                        break;
                    case ConsoleKey.DownArrow:
                        field_edit_index++;
                        break;
                    case ConsoleKey.UpArrow:
                        field_edit_index--;
                        break;
                    case ConsoleKey.Escape:
                        return recordChanged;
                }
                }
                catch (Exception ex)
                {
                    Console.SetCursorPosition(0, y + fields.Count + 2);
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("Error: " + ex.Message);
                    Console.WriteLine("Press any key to continue...");
                    Console.ForegroundColor = ConsoleColor.White;
                    Console.ReadKey(true);
                }
                finally
                {
                    Console.SetCursorPosition(0, y + fields.Count + 2);
                    Console.WriteLine(new string(' ', Console.WindowWidth));
                }

            }
            while (true);
        }
        protected void Draw()
        {
            int x, y;
            (x, y) = Console.GetCursorPosition();
            int i = 0;
            foreach (KeyValuePair<string, Field> kv in fields)
            {
                Field field = kv.Value;
                field.Focus = (i++ == field_edit_index);
                field.Draw(x, y++);
            }
            Console.WriteLine();
        }

        int getLongestTitleLength()
        {
            int longest = 0;
            foreach (KeyValuePair<string, Field> kv in fields)
            {
                Field field = kv.Value;
                longest = Math.Max(field.Title.Length, longest);
            }
            return longest;
        }
    }
    public abstract class Field
    {
        public string Title { get; set; }
        public string Property { get; set; }
        public abstract object Value { get; set; }
        public bool Focus { get; set; }
        public int FieldWidth { get; set; } = 20;
        public int LabelWidth { get; set; } = 20;
        public abstract void Draw(int left, int top);
        public abstract void Enter();

    }


    public class TextBox : Field
    {
        string value;
        public override object Value { get { return value; } set { this.value = value.ToString(); } }
        protected int left = 0;
        protected int top = 0;

        public virtual bool Validate(String input)
        {
            // Textbox accepts everything, override to limit input
            return true;
        }
        public override void Enter()
        {
            bool ok;
            string input;
            Console.CursorVisible = true;

            Console.SetCursorPosition(left + LabelWidth, top);
            Screen.ColorEdit();
            Console.WriteLine(Value);
            do
            {
                Console.SetCursorPosition(left + LabelWidth, top);
                input = Console.ReadLine();
                ok = Validate(input);
                if (!ok)
                {
                    Console.SetCursorPosition(left + LabelWidth, top);
                    Screen.ColorError();
                    Console.WriteLine(input);
                }
            } while (!ok);
            Value = input;
            Console.CursorVisible = false;
            Screen.ColorDefault();
        }
        public override void Draw(int left, int top)
        {
            this.left = left;
            this.top = top;
            Console.SetCursorPosition(left, top);
            Console.Write("{0,-" + LabelWidth + "}", Title);
            if (Focus)
                Screen.ColorFocus();
            else
                Screen.ColorField();


            Console.Write($"{{0,-{FieldWidth}}}", this);
            Screen.ColorDefault();
        }

        public override string ToString()
        {
            return value == null ? "" : value.ToString();
        }

    }
    public class IntBox : TextBox
    {
        int value;
        public override object Value
        {
            get => value;
            set
            {
                int.TryParse(value.ToString(), out this.value);
            }
        }
        public override string ToString()
        {
            return value.ToString();
        }
        public override bool Validate(String input)
        {
            return int.TryParse(input.ToString(), out _);
        }

    }

    public class DoubleBox : TextBox
    {
        double value;
        public override object Value
        {
            get => value;
            set
            {
                double.TryParse(value.ToString(), out this.value);
            }
        }
        public override string ToString()
        {
            return value.ToString();
        }

    }
    public class DecimalBox : TextBox
    {
        decimal value;
        public override object Value
        {
            get => value;
            set
            {
                decimal.TryParse(value.ToString(), out this.value);
            }
        }
        public override string ToString()
        {
            return value.ToString();
        }

    }

    public class SelectBox : Field
    {
        object value;
        public override object Value { get { return value; } set { this.value = value; } }
        public Dictionary<string, object> Options = new();
        int left = 0;
        int top = 0;
        int index = 0;
        public override void Enter()
        {
            bool stop = false;
            object currentValue = value;
            Func<int, object> valueByIndex = (idx) =>
            {
                int ii = 0;
                foreach (KeyValuePair<string, object> kv in Options)
                {
                    if (ii++ == idx) return kv.Value;
                }
                return null;
            };
            do
            {
                int i = 0;

                foreach (KeyValuePair<string, object> kv in Options)
                {
                    Console.SetCursorPosition(left + LabelWidth, top + 1 + i);
                    if (i == index)
                        Screen.ColorFocus();
                    else
                        Screen.ColorDefault();

                    Console.Write("{0," + FieldWidth + "}", kv.Key);
                    i++;
                }


                ConsoleKey key = Console.ReadKey(true).Key;
                index = Math.Clamp(index, 0, Options.Count - 1);
                switch (key)
                {
                    case ConsoleKey.Enter:
                        value = valueByIndex(index);
                        //Need to clear and redraw this screen
                        Screen.Clear();
                        stop = true;
                        break;
                    case ConsoleKey.UpArrow:
                        index--;
                        break;
                    case ConsoleKey.DownArrow:
                        index++;
                        break;
                    case ConsoleKey.Escape:
                        stop = true;
                        break;
                }
            } while (!stop);

            Console.CursorVisible = false;
            Screen.ColorDefault();
        }
        public override void Draw(int left, int top)
        {
            this.left = left;
            this.top = top;
            Console.SetCursorPosition(left, top);
            Console.Write("{0,-" + LabelWidth + "}", Title);
            if (Focus)
                Screen.ColorFocus();
            else
                Screen.ColorField();


            Console.Write($"{{0,-{FieldWidth}}}", Value);
            Screen.ColorDefault();
        }
    }

    public class SearchBox : Field
    {
        const int MaxVisibleResults = 5;

        object value;
        string display = "";
        public Func<string, List<(string, object)>> Search;
        public override object Value
        {
            get { return value; }
            set
            {
                this.value = value;
                this.display = value?.ToString() ?? "";
            }
        }
        int left = 0;
        int top = 0;
        int index = 0;
        int lastDrawnRows = 0;
        List<(string display, object value)> searchResults = new();

        public override void Enter()
        {
            if (Search == null) return;

            string searchTerm = "";
            searchResults = new();
            index = 0;
            bool stop = false;

            Console.CursorVisible = true;

            do
            {
                DrawSearchTerm(searchTerm);
                DrawResults();

                ConsoleKeyInfo keyInfo = Console.ReadKey(true);
                switch (keyInfo.Key)
                {
                    case ConsoleKey.Enter:
                        if (searchResults.Count > 0)
                        {
                            var picked = searchResults[index];
                            value = picked.value;
                            display = picked.display;
                        }
                        stop = true;
                        break;
                    case ConsoleKey.Escape:
                        stop = true;
                        break;
                    case ConsoleKey.UpArrow:
                        if (index > 0) index--;
                        break;
                    case ConsoleKey.DownArrow:
                        int visible = Math.Min(searchResults.Count, MaxVisibleResults);
                        if (index < visible - 1) index++;
                        break;
                    case ConsoleKey.Backspace:
                        if (searchTerm.Length > 0)
                        {
                            searchTerm = searchTerm[..^1];
                            searchResults = searchTerm.Length > 0
                                ? Search(searchTerm) ?? new()
                                : new();
                            index = 0;
                        }
                        break;
                    default:
                        if (!char.IsControl(keyInfo.KeyChar))
                        {
                            searchTerm += keyInfo.KeyChar;
                            searchResults = Search(searchTerm) ?? new();
                            index = 0;
                        }
                        break;
                }
            } while (!stop);

            ClearResultArea();
            Console.CursorVisible = false;
            Screen.ColorDefault();
        }

        void DrawSearchTerm(string term)
        {
            Console.SetCursorPosition(left + LabelWidth, top);
            Screen.ColorEdit();
            string shown = term.Length > FieldWidth ? term[^FieldWidth..] : term;
            Console.Write(shown.PadRight(FieldWidth));
            Screen.ColorDefault();
            Console.SetCursorPosition(left + LabelWidth + shown.Length, top);
        }

        void DrawResults()
        {
            int visible = Math.Min(searchResults.Count, MaxVisibleResults);
            for (int i = 0; i < visible; i++)
            {
                Console.SetCursorPosition(left + LabelWidth, top + 1 + i);
                if (i == index) Screen.ColorFocus();
                else Screen.ColorField();
                string text = searchResults[i].display ?? "";
                if (text.Length > FieldWidth) text = text[..FieldWidth];
                Console.Write(text.PadRight(FieldWidth));
            }
            for (int i = visible; i < lastDrawnRows; i++)
            {
                Console.SetCursorPosition(left + LabelWidth, top + 1 + i);
                Screen.ColorDefault();
                Console.Write(new string(' ', FieldWidth));
            }
            lastDrawnRows = visible;
            Screen.ColorDefault();
        }

        void ClearResultArea()
        {
            Screen.ColorDefault();
            for (int i = 0; i < lastDrawnRows; i++)
            {
                Console.SetCursorPosition(left + LabelWidth, top + 1 + i);
                Console.Write(new string(' ', FieldWidth));
            }
            lastDrawnRows = 0;
        }

        public override void Draw(int left, int top)
        {
            this.left = left;
            this.top = top;
            Console.SetCursorPosition(left, top);
            Console.Write("{0,-" + LabelWidth + "}", Title);
            if (Focus) Screen.ColorFocus();
            else Screen.ColorField();

            string text = display ?? "";
            if (text.Length > FieldWidth) text = text[..FieldWidth];
            Console.Write("{0,-" + FieldWidth + "}", text);
            Screen.ColorDefault();
        }
    }
}

