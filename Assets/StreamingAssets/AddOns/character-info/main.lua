-- A draggable, skinned player panel with health/mana bars and manual refresh.
local elapsed = 0
local refreshes = 0
local function refresh()
    local player = game.observe("player").data
    if not player.available then
        ui.set_widget_text("character", "summary", player.reason or "等待角色数据……")
        ui.set_widget_text("character", "location", "尚未选择角色")
        ui.set_progress("character", "health", 0)
        ui.set_progress("character", "mana", 0)
        return
    end
    ui.set_widget_text("character", "summary", (player.name or "未知角色") .. "  等级 " .. tostring(player.level or 0))
    ui.set_widget_text("character", "health", "生命 " .. tostring(player.hp or 0) .. " / " .. tostring(player.maxHp or 0))
    ui.set_widget_text("character", "mana", "法力 " .. tostring(player.mp or 0) .. " / " .. tostring(player.maxMp or 0))
    ui.set_progress("character", "health", (player.hp or 0) / math.max(1, player.maxHp or 0))
    ui.set_progress("character", "mana", (player.mp or 0) / math.max(1, player.maxMp or 0))
    ui.set_widget_text("character", "location", "地图 " .. tostring(player.mapId or "?") .. "  (" .. tostring(player.x or 0) .. ", " .. tostring(player.y or 0) .. ")")
end
function on_load()
    refreshes = storage.get("manualRefreshes") or 0
end
function on_enable()
    ui.widget_panel({id = "character", title = "角色信息", width = 304, height = 230,
        x = 28, y = 96, draggable = true, widgets = {
            {id = "summary", kind = "label", text = "正在加载……", x = 14, y = 14, width = 276, height = 28},
            {id = "health", kind = "progress", text = "生命", x = 14, y = 50, width = 276, height = 26},
            {id = "mana", kind = "progress", text = "法力", x = 14, y = 82, width = 276, height = 26},
            {id = "location", kind = "label", text = "", x = 14, y = 118, width = 276, height = 36},
            {id = "refresh", kind = "button", text = "刷新", x = 14, y = 162, width = 120, height = 28}
        }})
    refresh()
end
function on_update(dt)
    elapsed = elapsed + dt
    if elapsed >= 0.25 then elapsed = 0; refresh() end
end
function on_event(event)
    if event.name == "scene.changed" or event.name == "events.resync_required" then refresh() end
end
function on_ui(event)
    if event.name == "click" and event.widgetId == "refresh" then
        refreshes = refreshes + 1
        storage.set("manualRefreshes", refreshes)
        refresh()
    end
end
