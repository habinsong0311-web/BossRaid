using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.InputSystem;
using SF = UnityEngine.SerializeField;
public class SkillTest : MonoBehaviour
{
    private static readonly int LightningHash = Animator.StringToHash("Ground");
    private static readonly int LightningBoltHash = Animator.StringToHash("Hand");
    private static readonly int TestHash = Animator.StringToHash("Horizontal");
    private static readonly int Test2Hash = Animator.StringToHash("Vertical");
    [SF] private SkillSpawner skillSpawner;
    [SF] private Animator animator;
    [SF] private SkillPointer pointer;
    private float skill1Cooldown = 3;
    
    private float skill2Cooldown = 10;
    private float skill3Cooldown = 20;
    private float skill4Cooldown = 15;

    private float skill5Cooldown = 2;
    private float skill6Cooldown = 7;

    private float nowCoolSk1 = 0;
    private float nowCoolSk2 = 0;
    private float nowCoolSk3 = 0;
    private float nowCoolSk4 = 0;
    private float nowCoolSk5 = 0;
    private float nowCoolSk6 = 0;

    public bool isMeleeTest = true;

    private bool isSkillTargeting;

    private async UniTask PointerCk()
    {
        if (pointer == null) return;

        isSkillTargeting = true;
        Vector3? data = await pointer.ActiveSkillPointer();

        isSkillTargeting = false;

        if (data != null)
        {
            bool val = TriggerTestSkill(1003, data.Value);

            if (!val) return;

            nowCoolSk4 = Time.time + skill4Cooldown;
        }
    }

    private void Update()
    {
        if (Keyboard.current == null) return;
        if (animator.IsInTransition(0)) return;

        if (!isMeleeTest) { 
            if(Keyboard.current.eKey.wasPressedThisFrame && !isSkillTargeting && nowCoolSk4 < Time.time)
            {
                PointerCk().Forget();
            }

            if (Keyboard.current.qKey.wasPressedThisFrame && nowCoolSk5 < Time.time)
            {
                bool val = TriggerTestSkill(1004);
                if (!val) return;

                nowCoolSk5 = Time.time + skill5Cooldown;
            }

            if (Keyboard.current.rKey.wasPressedThisFrame && nowCoolSk6 < Time.time)
            {
                bool val = TriggerTestSkill(1005);
                if (!val) return;

                nowCoolSk6 = Time.time + skill6Cooldown;
            }
        }

        if (Keyboard.current.spaceKey.wasPressedThisFrame && nowCoolSk1 < Time.time)
        {
            bool val = TriggerTestSkill(1000);
            if (!val) return;

            nowCoolSk1 = Time.time + skill1Cooldown;
        }

        if (Keyboard.current.leftAltKey.wasPressedThisFrame && nowCoolSk2 < Time.time)
        {
            bool val = TriggerTestSkill(1001);
            if (!val) return;

            nowCoolSk2 = Time.time + skill2Cooldown;
            
        }

        if (Keyboard.current.leftCtrlKey.wasPressedThisFrame && nowCoolSk3 < Time.time)
        {
            bool val = TriggerTestSkill(1002);
            if (!val) return;

            nowCoolSk3 = Time.time + skill3Cooldown;

        }
    }

    private bool TriggerTestSkill(int id)
    {
        bool success = skillSpawner.UseSkill( id);

        if (!success) return false;

        string SkillType
            = SkillDataLoader.Instance.SkillData[id].AnimationType.ToString();

        animator.SetTrigger(SkillType);

        Debug.Log($"[테스트] {id}번 스킬 시전!");

        return true;
    }

    private bool TriggerTestSkill(int id, Vector3 pos)
    {
        bool success = skillSpawner.UseSkillAOE(id, pos);

        if (!success) return false;

        string SkillType 
            = SkillDataLoader.Instance.SkillData[id].AnimationType.ToString();

        animator.SetTrigger(SkillType);

        Debug.Log($"[테스트] {id}번 스킬 시전!");

        return true;
    }
}
