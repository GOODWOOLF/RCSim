@tool
extends EditorScript

func _run():
    var scene = load("res://RobotRR.res")

    if scene is PackedScene:
        var error = ResourceSaver.save(
            scene,
            "res://RobotRR.tscn"
        )
        print("Результат: ", error)
    else:
        print("Ошибка: файл не является PackedScene")