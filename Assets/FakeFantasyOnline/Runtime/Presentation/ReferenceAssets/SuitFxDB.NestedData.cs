using System;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{

public static partial class SuitFxDB
{
    [Serializable]
    private class Item
    {
        public int id;
        public string cat;
        public int model;
        public int key;
    }

    [Serializable]
    private class Spec
    {
        public int[] k;
        public string fx;
    }

    [Serializable]
    private class JFile
    {
        public Item[] items;
        public Spec[] specs;
    }

    [Serializable]
    private class FuseItem
    {
        public int id;
        public string name;
    }

    [Serializable]
    private class FuseFile
    {
        public FuseItem[] items;
    }

    [Serializable]
    private class DragonItem
    {
        public int id;
        public string slot;
    }

    [Serializable]
    private class DragonFile
    {
        public DragonItem[] items;
    }

    [Serializable]
    private class DualFile
    {
        public int[] models;
    }
}

}
