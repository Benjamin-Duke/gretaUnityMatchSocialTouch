using thrift.gen_csharp; /**
 *
 * @author Ken
 */


namespace thrift.services
{
    public class SimpleComHandler : SimpleCom.Iface
    {
        private readonly Receiver receiver;
        private Message message;

        public SimpleComHandler(Receiver receiver)
        {
            message = new Message();
            this.receiver = receiver;
        }


        public void send(Message m)
        {
            message = m;

            //   if(message.Type!=null)
            //       Debug.Log("message received by server:"+message.Type);

            receiver.perform(message);
        }


        public bool isStarted()
        {
            return receiver.isConnected();
        }
    }
}