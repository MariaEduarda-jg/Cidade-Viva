// Narração do cartão de verbete no WebGL (ver VozDoNavegador.cs): lê o texto com a síntese de voz
// do navegador, em português do Brasil. Nada aqui pode lançar erro: sem voz em português, o jogo
// só esconde o botão "Ouvir".
mergeInto(LibraryManager.library, {
  $CidadeVivaVoz: {
    // Devolve a voz pt-BR do navegador; se não houver, outra voz em português; se não houver, null.
    // As vozes carregam depois da página, então a lista pode vir vazia nas primeiras chamadas.
    escolher: function () {
      try {
        if (typeof window === "undefined" || !window.speechSynthesis || typeof SpeechSynthesisUtterance === "undefined") {
          return null;
        }
        var vozes = window.speechSynthesis.getVoices() || [];
        var outraEmPortugues = null;
        for (var i = 0; i < vozes.length; i++) {
          var idioma = (vozes[i].lang || "").replace("_", "-").toLowerCase();
          if (idioma === "pt-br") {
            return vozes[i];
          }
          if (!outraEmPortugues && idioma.indexOf("pt") === 0) {
            outraEmPortugues = vozes[i];
          }
        }
        return outraEmPortugues;
      } catch (erro) {
        return null;
      }
    }
  },

  CidadeVivaVozDisponivel__deps: ["$CidadeVivaVoz"],
  CidadeVivaVozDisponivel: function () {
    return CidadeVivaVoz.escolher() ? 1 : 0;
  },

  CidadeVivaVozFalar__deps: ["$CidadeVivaVoz"],
  CidadeVivaVozFalar: function (texto) {
    try {
      var voz = CidadeVivaVoz.escolher();
      if (!voz) {
        return;
      }
      var fala = new SpeechSynthesisUtterance(UTF8ToString(texto));
      fala.voice = voz;
      fala.lang = "pt-BR";
      fala.rate = 0.9;
      window.speechSynthesis.cancel();
      window.speechSynthesis.speak(fala);
    } catch (erro) {
      console.warn("Cidade Viva: nao foi possivel narrar o texto.", erro);
    }
  },

  CidadeVivaVozParar: function () {
    try {
      if (typeof window !== "undefined" && window.speechSynthesis) {
        window.speechSynthesis.cancel();
      }
    } catch (erro) {
    }
  }
});
