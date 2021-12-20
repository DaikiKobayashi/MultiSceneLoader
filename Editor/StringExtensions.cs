using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MultiSceneLoader
{
    public static class StringExtensions
    {
        public static string Coloring(this string str, string color)
        {
            return string.Format("<color={0}>{1}</color>", color, str);
        }
        public static string Red(this string str)
        {
            return str.Coloring("red");
        }
        public static string Green(this string str)
        {
            return str.Coloring("green");
        }
        public static string Blue(this string str)
        {
            return str.Coloring("blue");
        }
        public static string Resize(this string str, int size)
        {
            return string.Format("<size={0}>{1}</size>", size, str);
        }
        public static string Medium(this string str)
        {
            return str.Resize(11);
        }
        public static string Small(this string str)
        {
            return str.Resize(9);
        }
        public static string Large(this string str)
        {
            return str.Resize(16);
        }
        public static string Bold(this string str)
        {
            return string.Format("<b>{0}</b>", str);
        }
        public static string Italic(this string str)
        {
            return string.Format("<i>{0}</i>", str);
        }
    }
}