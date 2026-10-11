// Chamado por Perfis.Salvar no WebGL: grava no IndexedDB do navegador o que foi escrito
// em Application.persistentDataPath, pra o perfil não se perder ao fechar a aba.
mergeInto(LibraryManager.library, {
  CidadeVivaSincronizarArquivos: function () {
    FS.syncfs(false, function (erro) {
      if (erro) {
        console.warn("Cidade Viva: nao foi possivel guardar o perfil no navegador.", erro);
      }
    });
  }
});
