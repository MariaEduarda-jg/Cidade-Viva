using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace CidadeViva.EditorArte
{
    // Colisores pro cenário onde a criança anda (Hub), pra ela não atravessar prédios, árvores, carros,
    // objetos e pessoas (2026-11-22, pedido da usuária). Os prefabs de arte vêm sem collider; aqui cada
    // coisa ganha um:
    //  - árvore e palmeira: só o tronco (cápsula fina), pra copa não bloquear;
    //  - outro prefab (prédio, carro, pessoa, banco, lixeira...): uma caixa do tamanho dele, cortada a 2 m de altura;
    //  - forma solta da paleta (cubo, cilindro...): o collider do próprio formato.
    // Fica de fora: o que é chão ou pintura (baixo, menos de 30 cm), o que está no alto (começa acima de 1,8 m),
    // o que tiver nome de chão/rua/água, o jogador, a Ponte JK e o que estiver perto do marcador de cada área
    // (pra criança sempre conseguir entrar).
    public static class ColisoresDoCenario
    {
        static readonly string[] NomesDeChao =
        {
            "Chao", "Terreno", "Asfalto", "Calcada", "Faixa", "Pintura", "Rio", "Agua", "Margem", "Lago", "Laguinho",
            "Grama", "Piso", "Patio", "Lama", "Marcador", "Anel", "Pista", "Ciclovia", "Ciclofaixa", "Traco", "Listra",
            "Sombra", "Poca", "Mancha", "Tapete", "Papeis", "Reflexo", "Limites", "Ponte_JK", "Luz", "Passaro",
        };

        public static int Criar(Transform raiz, IEnumerable<Vector3> livres, float raioLivre = 2.8f)
        {
            var pontosLivres = livres.ToList();
            int n = 0;
            var feitos = new HashSet<GameObject>();
            foreach (Renderer r in raiz.GetComponentsInChildren<Renderer>(true))
            {
                if (r is ParticleSystemRenderer || r is SkinnedMeshRenderer && r.GetComponentInParent<CharacterController>(true) != null) continue;
                if (r.GetComponentInParent<CharacterController>(true) != null || r.GetComponentInParent<Canvas>(true) != null) continue;
                if (TemNomeDeChao(r.transform, raiz)) continue;
                // Malha juntada (OtimizacaoWebGL.Combinar) ou peça de algo que já tem colisor próprio (monumento,
                // ministério, bloco sobre pilotis): quem montou cuidou do colisor.
                if (r.name.StartsWith("Combinado_") || TemColisorAcima(r.transform, raiz)) continue;

                var instancia = PrefabUtility.GetOutermostPrefabInstanceRoot(r.gameObject);
                GameObject dono = instancia != null && instancia.transform.IsChildOf(raiz) ? instancia : r.gameObject;
                if (!feitos.Add(dono)) continue;
                if (dono.GetComponentInChildren<Collider>(true) != null) continue;

                Bounds b = Medir(dono);
                if (b.size.y < .3f || b.min.y > 1.8f) continue;
                var centro = new Vector2(b.center.x, b.center.z);
                if (pontosLivres.Any(p => Vector2.Distance(centro, new Vector2(p.x, p.z)) < raioLivre)) continue;

                string caminho = instancia != null ? PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(dono) ?? "" : "";
                if (caminho.Contains("Arvore") || caminho.Contains("Palmeira"))
                {
                    var cap = dono.AddComponent<CapsuleCollider>();
                    Vector3 baseLocal = dono.transform.InverseTransformPoint(new Vector3(b.center.x, b.min.y, b.center.z));
                    float esc = Mathf.Max(.01f, dono.transform.lossyScale.y);
                    cap.radius = .35f / Mathf.Max(.01f, dono.transform.lossyScale.x);
                    cap.height = 2f / esc;
                    cap.center = baseLocal + Vector3.up * (1f / esc);
                }
                else if (instancia != null)
                {
                    // Prédio (mais de 4 m): caixa inteira, pra câmera também não entrar nele. O resto, até 2 m.
                    var caixa = dono.AddComponent<BoxCollider>();
                    Bounds local = MedirLocal(dono, b.size.y > 4f ? b.max.y : b.min.y + 2f);
                    caixa.center = local.center;
                    caixa.size = local.size;
                }
                else
                {
                    var mf = dono.GetComponent<MeshFilter>();
                    if (mf == null || mf.sharedMesh == null) continue;
                    if (b.size.y > 2.2f && b.min.y < .5f)
                    {
                        // Forma alta (prédio de formas, pilar): caixa só até 2 m, pra não segurar nada no alto.
                        var caixa = dono.AddComponent<BoxCollider>();
                        Bounds local = MedirLocal(dono, b.min.y + 2f);
                        caixa.center = local.center;
                        caixa.size = local.size;
                    }
                    else dono.AddComponent<BoxCollider>();
                }
                n++;
            }
            return n;
        }

        static bool TemColisorAcima(Transform t, Transform raiz)
        {
            for (var x = t.parent; x != null; x = x.parent)
            {
                foreach (var c in x.GetComponents<Collider>()) if (!c.isTrigger) return true;
                if (x == raiz) break;
            }
            return false;
        }

        static bool TemNomeDeChao(Transform t, Transform raiz)
        {
            for (var x = t; x != null && x != raiz; x = x.parent)
                foreach (var nome in NomesDeChao)
                    if (x.name.StartsWith(nome) || x.name.Contains("_" + nome)) return true;
            return false;
        }

        static Bounds Medir(GameObject go)
        {
            var rs = go.GetComponentsInChildren<Renderer>(true).Where(x => !(x is ParticleSystemRenderer)).ToArray();
            var b = rs[0].bounds;
            foreach (var r in rs) b.Encapsulate(r.bounds);
            return b;
        }

        // Caixa no espaço local do objeto, cortada na altura (mundo) "teto".
        static Bounds MedirLocal(GameObject go, float teto)
        {
            var t = go.transform;
            bool primeiro = true;
            var local = new Bounds();
            foreach (var r in go.GetComponentsInChildren<Renderer>(true))
            {
                if (r is ParticleSystemRenderer) continue;
                var b = r.bounds;
                if (b.min.y > teto) continue;
                var max = new Vector3(b.max.x, Mathf.Min(b.max.y, teto), b.max.z);
                for (int i = 0; i < 8; i++)
                {
                    var canto = new Vector3((i & 1) == 0 ? b.min.x : max.x, (i & 2) == 0 ? b.min.y : max.y, (i & 4) == 0 ? b.min.z : max.z);
                    var p = t.InverseTransformPoint(canto);
                    if (primeiro) { local = new Bounds(p, Vector3.zero); primeiro = false; }
                    else local.Encapsulate(p);
                }
            }
            return local;
        }
    }
}
