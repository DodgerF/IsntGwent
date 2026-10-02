// Мост в браузер для входа через itch.io. Всё, что ему нужно уметь, — открыть страницу
// разрешения в отдельном окне. Токен сюда не попадает вовсе: его забирает страница возврата
// на нашем домене и отдаёт нашему серверу.
mergeInto(LibraryManager.library, {

  // 1 — окно открыто, 2 — откроется по следующему клику игрока, 0 — не вышло вовсе.
  ItchOpenLogin: function (urlPtr) {
    var url = UTF8ToString(urlPtr);
    var features = "width=560,height=760,menubar=no,toolbar=no,location=yes";

    try {
      var popup = window.open(url, "itch_oauth", features);

      if (popup && !popup.closed) {
        popup.focus();
        return 1;
      }
    } catch (e) {
      // Ниже есть запасной путь.
    }

    // Браузер открывает окно только внутри настоящего жеста, а Unity отдаёт нажатие
    // в свой цикл уже после того, как жест кончился, — поэтому ловим следующий клик
    // по странице и открываем оттуда.
    try {
      var open = function () {
        document.removeEventListener("pointerup", open, true);
        document.removeEventListener("click", open, true);

        var second = window.open(url, "itch_oauth", features);
        if (second) second.focus();
      };

      document.addEventListener("pointerup", open, true);
      document.addEventListener("click", open, true);

      return 2;
    } catch (e) {
      return 0;
    }
  }
});
