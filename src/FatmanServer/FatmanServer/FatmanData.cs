namespace FatmanServer;

internal static class FatmanData
{
	public const string CloneTpl = "60bf74184a63fc79b60c57f6";

	public const string ParentId = "5448e54d4bdc2dcc718b4568";

	public const string HandbookParent = "5b5f701386f774093f2ecf0f";

	public const string RagmanId = "5ac3b934156ae10c4430e83c";

	public const string Roubles = "5449016a4bdc2d6f028b456f";

	public const string KillaTpl = "677df1d45982eada0cb7853d";

	public const string BossKilla = "bosskilla";

	public static readonly string[] ArmorColliders = new string[20]
	{
		"RibcageUp", "Pelvis", "LeftUpperArm", "LeftForearm", "RightUpperArm", "RightForearm", "LeftThigh", "LeftCalf", "RightThigh", "RightCalf",
		"NeckFront", "NeckBack", "RightSideChestUp", "LeftSideChestUp", "SpineTop", "SpineDown", "PelvisBack", "RightSideChestDown", "LeftSideChestDown", "RibcageLow"
	};

	public static readonly (string Tpl, string InternalName, string Bundle, string EnName, string? AssortId, double? Discard, bool Sell, string Desc)[] Items = new (string Tpl, string InternalName, string Bundle, string EnName, string? AssortId, double? Discard, bool Sell, string Desc)[6]
	{
		("6775e7048cf9bdcac82b5b39", "R61_Fatman_Black", "black", "R61 Fatman Full Body Armor (Black)", "6773b9fbbb3827160f8299a8", null, true, ""),
		("6775fe7192d0a7b7b0b88026", "R61_Fatman_Digital_Green", "digital_green", "R61 Fatman Full Body Armor (Digital Green)", "677600fff82f91126af58c48", null, true, ""),
		("6775fe865b809be61cb989ec", "R61_Fatman_Digital_FDE", "fde", "R61 Fatman Full Body Armor (FDE)", "67760100404f31f59f39e765", null, true, ""),
		("6775feaa2d25489f3a0fc467", "R61_Fatman_Olive_Drab", "olive_drab", "R61 Fatman Full Body Armor (Olive Drab)", "6776010370666ce1b0ae59c0", null, true, ""),
		("6775ff864a0c09f9590e65b7", "R61_Fatman_White", "white", "R61 Fatman Full Body Armor (White)", "67760104ea7b422d0bfffeda", 1.0, true, ""),
		("677df1d45982eada0cb7853d", "R61_Fatman_Killa", "killa", "R61 Fatman Full Body Armor (Killa)", null, 1.0, false, "Some fatman armor that was worn by Killa")
	};
}
