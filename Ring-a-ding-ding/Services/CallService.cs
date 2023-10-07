using Windows.ApplicationModel.Calls;

namespace Ring_a_ding_ding.Services
{
    public class CallService
    {


        public CallService() { }

        public static void RingHandsfreeDevice()
        {
            var voipCallCoordinator = VoipCallCoordinator.GetDefault();
            VoipPhoneCall call = voipCallCoordinator.RequestNewAppInitiatedCall("liar://lies", "me", "555-555-5555", "btservice", VoipPhoneCallMedia.Audio);
        }
    }
}
