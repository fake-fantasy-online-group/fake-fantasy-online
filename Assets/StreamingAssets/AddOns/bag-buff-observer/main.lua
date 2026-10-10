-- Bounded bag and buff observations; no inventory actions are executed.
local elapsed = 0
local function refresh()
    local inventory = game.observe("inventory", {limit = 8}).data
    local lines = {}
    if inventory.available then
        table.insert(lines, "金币：" .. tostring(inventory.money or 0) .. "  负重：" .. tostring(inventory.weight or 0) .. "/" .. tostring(inventory.maxWeight or 0))
        for _, item in ipairs(inventory.items or {}) do
            table.insert(lines, (item.name or tostring(item.itemId)) .. " x" .. tostring(item.count or 0))
        end
        if inventory.truncated then table.insert(lines, "仅显示前 8 个物品格") end
        if #lines == 1 then table.insert(lines, "当前视图没有物品") end
    else
        table.insert(lines, inventory.reason or "等待背包数据……")
    end
    ui.set_widget_text("observer", "bag", table.concat(lines, "\n"))
    local buffs = game.observe("buffs", {limit = 8}).data
    lines = {}
    if buffs.available then
        for _, buff in ipairs(buffs.buffs or {}) do
            table.insert(lines, (buff.name or tostring(buff.skillId)) .. "  " .. tostring(buff.remainingSecondsAtReceipt or 0) .. " 秒（收到数据时）")
        end
        if #lines == 0 then table.insert(lines, "当前没有增益状态") end
        if buffs.truncated then table.insert(lines, "仅显示前 8 个增益") end
    else
        table.insert(lines, buffs.reason or "等待增益数据……")
    end
    ui.set_widget_text("observer", "buffs", table.concat(lines, "\n"))
end
function on_enable()
    ui.widget_panel({id = "observer", title = "背包与增益观察", width = 360, height = 500,
        anchor = "TopRight", x = 28, y = 96, draggable = true, widgets = {
            {id = "bag-title", kind = "label", text = "背包", x = 14, y = 12, width = 326, height = 24},
            {id = "bag", kind = "label", text = "正在加载……", x = 14, y = 42, width = 326, height = 204},
            {id = "buff-title", kind = "label", text = "增益（服务器上报时间）", x = 14, y = 250, width = 326, height = 24},
            {id = "buffs", kind = "label", text = "正在加载……", x = 14, y = 280, width = 326, height = 170}
        }})
    refresh()
end
function on_update(dt)
    elapsed = elapsed + dt
    if elapsed >= 1 then elapsed = 0; refresh() end
end
function on_event(event)
    if event.name == "scene.changed" or event.name == "events.resync_required" then refresh() end
end
