-- A minimal addon. Enable it in the in-game addon manager.
local elapsed = 0
local function refresh()
    local player = game.observe("player").data
    local text = "等待进入游戏世界……"
    if player.available then
        text = "欢迎，" .. (player.name or "冒险者") .. "!"
    end
    ui.set_text("greeting", text)
end
function on_enable()
    ui.panel({id = "greeting", title = "你好，世界", text = "正在加载……"})
    refresh()
end
function on_update(dt)
    elapsed = elapsed + dt
    if elapsed >= 1 then elapsed = 0; refresh() end
end
function on_event(event)
    if event.name == "scene.changed" or event.name == "events.resync_required" then refresh() end
end
-- The host owns panel disposal and event cleanup, including script failures.
