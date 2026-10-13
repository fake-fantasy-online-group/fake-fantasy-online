// Copy this file into a temporary directory under the paired server/server/cmd tree
// and run it with the fixture output directory argument. Uses production Go DTOs/encoder.
package main

import (
	"encoding/hex"
	"encoding/json"
	"github.com/fake-fantasy-online-group/fake-fantasy-online-server/server/internal/domain"
	"github.com/fake-fantasy-online-group/fake-fantasy-online-server/server/internal/protocol"
	"os"
	"path/filepath"
)

func main() {
	stat := domain.EndgameStatView{Attr: 34, Value: 9, Mode: 1}
	material := domain.EndgameMaterialView{Owned: 99, ItemID: 1900030001, Count: 7, Name: "星髓"}
	formation := domain.EndgameFormationView{ID: 9, Name: "太清宿阵", Description: "合宿试验", Channel: "攻", Runes: []int32{1900028001, 1900028002}, Stats: []domain.EndgameStatView{stat}, MinLevel: 50, Professions: 31, Slots: []int32{1, 25}, Active: true, InactiveReason: "fixture"}
	r := domain.EndgameResponse{
		Kind: "snapshot", RequestID: "full-fields-request-0001", OK: true, Error: "fixture", Message: "全字段合成测试", Revision: "19", CatalogVersion: "full-fixture-v1", Capabilities: []string{domain.EndgameV1}, Cursor: "32", NextCursor: "64", SnapshotID: "full-fields-snapshot", Chunk: 1, Chunks: 2,
		Items: []domain.EndgameItemView{{Tab: 2, Level: 50, Profession: 4, UID: "9007199254740993", ItemID: 22211, Location: "bag", Slot: 99, Revision: "18", UniqueID: 22211, UniqueRollBP: 12000, Name: "青冥承露", Description: "全字段器物", Trait: "职业道意试验", SocketCount: 2, Sockets: []domain.EndgameSocketView{{Name: "角木蛟", Index: 1, ItemID: 1900028001, UID: "9223372036854775807"}}, Stats: []domain.EndgameStatView{stat}, FormationID: 9, Active: true, InactiveReason: "fixture", Bound: true, Locked: true}},
		Runes: []domain.EndgameRuneView{{MinLevel: 12, Mansion: "角", ItemID: 1900028001, Name: "角木蛟", Description: "星篆试验", Quadrant: "青龙", Stats: []domain.EndgameStatView{stat}}}, Formations: []domain.EndgameFormationView{formation},
		Recipes: []domain.EndgameRecipeView{{ID: 11001, Name: "篆星", Description: "配方试验", Operation: "rune.inscribe", Money: "9007199254740993", Materials: []domain.EndgameMaterialView{material}, OutputItemID: 1900028001, TargetRequired: true}}, Materials: []domain.EndgameMaterialView{material},
		Quote: &domain.EndgameQuoteView{QuoteID: "quote-full-fixture", RecipeID: 11001, TargetUID: "9007199254740993", Revision: "19", Money: "9007199254740993", Materials: []domain.EndgameMaterialView{material}, ExpiresAt: "2099-01-01T00:00:00Z", Description: "报价试验"},
		Rift:  &domain.EndgameRiftView{Name: "洞天试验", KeyCount: 3, KeyItemID: 1900010001, Auras: []domain.EndgameRiftAuraView{{EntityID: "4294967295", Class: 3, Affixes: []string{"烈焰", "震环"}}}, Globes: []domain.EndgameRiftGlobeView{{EntityID: "4294967294", X: 321, Y: 654, Value: 900}}, RunID: "rift-full-fixture", Revision: "20", Phase: "completed", Tier: 150, Seed: "18446744073709551615", GeneratorVersion: 1, FloorIndex: 3, MapName: "试炼秘林", CanAdvance: true, Transitioning: true, ExitX: 720, ExitY: 960, MaxTier: 150, UnlockedTier: 150, Telegraphs: []domain.EndgameRiftTelegraphView{{ID: "tell-fixture-1", EntityID: "4294967295", Ability: "震环冲击", Shape: "ring", X: 100, Y: 200, EndX: 300, EndY: 400, Radius: 220, InnerRadius: 85, RemainingMS: 1800}}, Progress: 10000, ProgressMax: 10000, RemainingSeconds: 99, BossName: "劫影", Members: []domain.EndgameRiftMemberView{{Name: "同道", CharID: "9007199254740993", Level: 50, Eligible: true, Claimed: true}}, Claimable: true}, Result: map[string]string{"status": "saved"},
	}
	b, err := json.MarshalIndent(r, "", "  ")
	if err != nil {
		panic(err)
	}
	if len(os.Args) != 2 {
		panic("output directory required")
	}
	out := os.Args[1]
	if err = os.WriteFile(filepath.Join(out, "full-fields.json"), append(b, '\n'), 0644); err != nil {
		panic(err)
	}
	wire := protocol.EncodeEndgame(r)
	if wire == nil {
		panic("production encoding failed")
	}
	if err = os.WriteFile(filepath.Join(out, "full-fields.wire.hex"), []byte(hex.EncodeToString(wire)+"\n"), 0644); err != nil {
		panic(err)
	}
}
