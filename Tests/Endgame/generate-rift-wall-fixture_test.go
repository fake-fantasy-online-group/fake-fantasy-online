// Run as a test overlay in the paired server's internal/game/scene package.
// Calls the actual unexported authority implementation; no production source is changed.
package scene

import (
	"encoding/json"
	"github.com/fake-fantasy-online-group/fake-fantasy-online-server/server/internal/domain"
	"os"
	"testing"
)

func TestClientRiftWallGeometryFixture(t *testing.T) {
	output := os.Getenv("RIFT_WALL_GEOMETRY_FIXTURE")
	if output == "" {
		t.Skip("set RIFT_WALL_GEOMETRY_FIXTURE")
	}
	type point struct {
		X float64 `json:"x"`
		Y float64 `json:"y"`
	}
	type sample struct {
		Name    string                     `json:"name"`
		Wall    domain.EndgameRiftWallView `json:"wall"`
		From    point                      `json:"from"`
		To      point                      `json:"to"`
		Blocked bool                       `json:"blocked"`
	}
	out := struct {
		Clearance float64  `json:"clearance"`
		Cases     []sample `json:"cases"`
	}{Clearance: temporaryWallActorClearance}
	add := func(name string, wall temporaryWall, x, y, x2, y2 float64) {
		from := domain.Pos{MapID: 7, X: x, Y: y}
		to := domain.Pos{MapID: 7, X: x2, Y: y2}
		out.Cases = append(out.Cases, sample{Name: name, Wall: domain.EndgameRiftWallView{ID: wall.ID, X: wall.X, Y: wall.Y, X2: wall.X2, Y2: wall.Y2, Width: wall.Width}, From: point{x, y}, To: point{x2, y2}, Blocked: wall.pathBlocked(from, to, temporaryWallActorClearance)})
	}
	axis := temporaryWall{ID: "axis", Owner: "effect", MapID: 7, X: 100, Y: 60, X2: 100, Y2: 140, Width: 20, ExpiresAtMS: 15000}
	add("parallel-outside", axis, 77, 0, 77, 200)
	add("parallel-touching", axis, 78, 0, 78, 200)
	add("fast-crossing", axis, -10000, 100, 10000, 100)
	add("flat-end-outside", axis, 50, 47, 150, 47)
	add("flat-end-touching", axis, 50, 48, 150, 48)
	add("zero-step-inside", axis, 100, 100, 100, 100)
	add("zero-step-outside", axis, 50, 50, 50, 50)
	add("end-at-boundary", axis, 0, 100, 78, 100)
	add("start-at-boundary", axis, 78, 100, 0, 100)
	add("diagonal-corner", axis, 50, 20, 150, 120)
	add("long-parallel", axis, -1e7, 47, 1e7, 47)
	reverse := axis
	reverse.ID = "reversed"
	reverse.Y, reverse.Y2 = reverse.Y2, reverse.Y
	add("reverse-crossing", reverse, 0, 100, 200, 100)
	add("reverse-boundary", reverse, 78, 0, 78, 200)
	diagonal := temporaryWall{ID: "diagonal", Owner: "effect", MapID: 7, X: 0, Y: 0, X2: 80, Y2: 80, Width: 20, ExpiresAtMS: 15000}
	add("diagonal-crossing", diagonal, 0, 80, 80, 0)
	add("diagonal-outside", diagonal, 0, 120, 40, 120)
	add("diagonal-fast", diagonal, -10000, 40, 10000, 40)
	precise := temporaryWall{ID: "precise", Owner: "effect", MapID: 7, X: 120.25, Y: 97.5, X2: 184.375, Y2: 122.625, Width: 19.5, ExpiresAtMS: 15000}
	add("fractional-crossing", precise, 0, 110, 300, 110)
	add("fractional-clear", precise, 0, 160, 300, 160)
	add("fractional-origin", precise, 120.25, 97.5, 120.25, 97.5)
	add("fractional-end", precise, 184.375, 122.625, 184.375, 122.625)
	for _, w := range []temporaryWall{axis, diagonal, precise} {
		if !w.valid(7, 0) {
			t.Fatal("invalid source wall", w.ID)
		}
	}
	bytes, err := json.MarshalIndent(out, "", "  ")
	if err != nil {
		t.Fatal(err)
	}
	if err = os.WriteFile(output, append(bytes, '\n'), 0644); err != nil {
		t.Fatal(err)
	}
}
