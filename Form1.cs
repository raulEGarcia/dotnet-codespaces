using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace WindowsFormsApplication1
    {

  

       

        public partial class qsForm01 : Form
            {

            //--- fields
            const String queueTitle = "Queing Simulation Analytical Tool";
            string queueMainType0;
            string queueMainType1;
            string queueMainType2;

            static double steadyState1;

            int numberServers;
            int numberServerMin;
            int numberServerMax;

            int meanTransactionPerUnitTime;
            int meanCustomersArrivalPerUnitTime;

            long siteCapacity;

            //--- MESSAGE FLAGS
            bool serverIncMsg = false;
            bool capacityIncMsg1 = false; //limited
            bool capacityIncMsg2 = false; //unlimited

            public qsForm01( )
                {
                InitializeComponent( );
                }


            private void qsForm01_Load( object sender, EventArgs e )
                {
                this.currDate.Text = DateTime.Now.ToLongDateString( );
                }


            public void startSimulation1Btn_Click_1( object sender, EventArgs e )
                {
                //--- coordinate queue type selected internally and in drop down when not yet selected

                queueSteadyStateStatusTxt.Text = "N/A";
                
                string tx;
                int cboMMSKIndex = queueModelTypeCbox.SelectedIndex;
                if ( cboMMSKIndex == -1 ) cboMMSKIndex = 0;
                tx = this.queueModelTypeCbox.Items [ cboMMSKIndex ].ToString( );

                queueMainType0 = tx.Substring(0,4).Trim();
               
                if ( queueMainType0  == "M/M/" )
                    {

                    if ( tx == "M/M/1" )
                        {
                        //--- Kendall notation
                        queueNumberServers.Value = 1;
                        queueMainType1 = "MM10"; // 0 means unlim capacity, = limited capacity,  S means servers > 1
                        queueMainType2 = "MM Queues with 1-Server and Unlimited Capacity";
                        }
                    else if ( tx == "M/M/1/K" )
                        {
                        queueNumberServers.Value = 1;
                        queueMainType1 = "MM1K";
                        queueMainType2 = "MM Queues with 1-Server and Limited Capacity (K)";
                        }
                    else if ( tx == "M/M/S" )
                        {
                        queueMainType1 = "MMS0";
                        queueMainType2 = "MM Queues with S-Servers (>1) and Unlimited Capacity.\nNeed to specify S - number of Servers.";
                        }
                    else if ( tx == "M/M/S/K" )
                        {
                        queueMainType1 = "MMSK";
                        queueMainType2 = "MM Queues with S-servers (>1) and Limited Capacity (K)\nNeed to specify S - number of Servers and Capacity K > 0.";
                        }
                    else queueMainType1 = "Unkown queue type - error.";

                    MessageBox.Show( "Selected QUEUE model is:\n\n" + tx + " - " + queueMainType2, "Internal queue code is: " + queueMainType1 );

                    //--- clean up type for func's use
                
                    numberServers = (int) this.queueNumberServers.Value;

                    numberServerMin = (int) this.queueNumberServersMin.Value;
                    numberServerMax = (int) this.queueNumberServersMax.Value;

                    meanCustomersArrivalPerUnitTime = (int) this.queueMeanValueBirth.Value;
                    meanTransactionPerUnitTime = (int) this.queueMeanValueDeath.Value;

                    siteCapacity= (long) queueSiteMaxCapacity.Value;
                    string siteCapacityMsg;
                    if (siteCapacity==0) { siteCapacityMsg="Unlimited"; }
                    else { siteCapacityMsg=siteCapacity.ToString();}

                    //------------------------------- instance the model now

                    var queueModel = new quNS.QMF();
                    //-------------------------------

                    var rho = queueModel.rho(meanCustomersArrivalPerUnitTime, meanTransactionPerUnitTime, numberServers ) ;
                    queueSteadyStateValue1.Text = rho.ToString("F3" );
                    var rho1 = queueModel.rho1( meanCustomersArrivalPerUnitTime, meanTransactionPerUnitTime );
                    queueSteadyStateValue2.Text = rho1.ToString("F3" );

                 

                    if ( rho < 1 )
                        {
                      
                        queueSteadyStateStatusTxt.Text = "STEADY";
                        queueSteadyStateStatusTxt.ForeColor = Color.DarkBlue;

                        queueStateCommentsRTxt.Text = "Steady - The queue (waiting lines) will converge to a steady state.";
                        }
                    else 
                        {

                        queueSteadyStateStatusTxt.Text = "UNSTEADY";
                        queueSteadyStateStatusTxt.ForeColor = Color.Red;

                        queueStateCommentsRTxt.Text = "Unsteady - The queue (waiting lines) will grow without limit.\nMay need to increase the number of SERVERS to avoid this.";
                        }



                    var quData = 
                          "Model Queue Values are:  " + queueMainType1 +                      "\n" + 
                          "Arrivals average rate (lambda):  "+ meanCustomersArrivalPerUnitTime.ToString("F3") +"\n"+
                          "Departures average rate (mu):  " + meanTransactionPerUnitTime.ToString("F3") + "\n"+
                          "Number of Servers (S):  " + numberServers.ToString() + "\n" +
                          "Site Maximum Capacity (K):  " + siteCapacityMsg;

                   


                    MessageBox.Show( "The calc rho is:  " + rho.ToString( ) + "\n\n" + quData, queueMainType2 ); 
                    //MMS.queueCalc( numberServerMin, meanTransactionPerUnitTime, meanCustomersArrivalPerUnitTime );
                    
                    var quProbLim =20;

                  

                    var servers = (double) numberServers;
                    var K = (double) queueSiteMaxCapacity.Value;
                    double[] quProbDataNum = new double[quProbLim]; //- for numbers
                    string[] quProbDataStr = new string[quProbLim]; //-- for labels
                    const string probMsgFmt ="The Probability of a Population of:  {0} is:    {1}%";
                    string rtboxLine = "";

                    popnProbPnRTxtBox.Text = quData + "\n"; //--- header data for rich text box


                    var r = (double) rho;
                    var r1 = (double) rho1;
                    //--- the r passes the results of the lambda and mu

                    for ( int n=0; n < quProbLim; n++ )
                        {
                        //quPn( double n, double r, double servers, double K ) 

                         quProbDataNum[n] = queueModel.quPn( n, r, servers, K ) * 100;
                         rtboxLine = string.Format( probMsgFmt, n.ToString("000"), quProbDataNum [ n ].ToString( "F3" ));
                         popnProbPnRTxtBox.Text = popnProbPnRTxtBox.Text + "\n" + rtboxLine;
                        }
                    }
                else
                    {
                    MessageBox.Show( "This QUIEING (Wating lines) ANALYTICAL Modeling version can process\nM/M/1 to M/M/S/K (Markovian stockastic B/D processes) queues\nArrivals Poisson and Departures exponentail types. ",queueTitle );
                    }


                }

        

            private void quServersChanged( object sender, EventArgs e )
                {

                if ( !serverIncMsg )
                    {
                    //MessageBox.Show( "IF the numner of SERVERS is increased from 1 (one)\n" +
                    //               "- then 1st choose a model type of M/M/S - Unlimited capacity, or\n" +
                    //               "- then 1st choose a model type of M/M/S/K - Limited capacity, and\n" +
                    //               "  then also specify the Cacacity K > 0 too.", "Coordinate Values of S and K with the Queue Model" );
                    }
                serverIncMsg = true;
                }

            private void capacityChanged( object sender, EventArgs e )
                {

                if ( queueSiteMaxCapacity.Value == 0 )
                {    queueCapacityOptionCbox.SelectedIndex = queueCapacityOptionCbox.Items.IndexOf( "UNLIMITED" );  }
                else
                {    queueCapacityOptionCbox.SelectedIndex = queueCapacityOptionCbox.Items.IndexOf( "LIMITED" ); }

                }
             

            private void queueCapacityOptionCbox_SelectedIndexChanged( object sender, EventArgs e )
                {
                string item1, item2, item3;
                item1 = queueCapacityOptionCbox.SelectedItem.ToString( );
                if ( item1 == null ) return;

                if ( item1.Length == 7 && item1.Substring( 0, 7 ) == "LIMITED" && capacityIncMsg1 ) return;
                
                if ( item1.Length == 9 && item1.Substring( 0, 9 ) == "UNLIMITED" && capacityIncMsg2 ) return;

                if ( !capacityIncMsg1 && item1.Length == 7 )
                    {
                
                    item2 = item1.Substring( 0, 7 );// LIMITED-

                    if ( item2 == "LIMITED" )
                        {
                        //MessageBox.Show( "IF the site CAPACITY (K) coment is changed to LIMITED of some kind\n" +
                        //               "- then 1st choose a model type of M/M/1/K or M/M/S/K - Limited capacity,and\n" +
                        //               "  then also specify the Cacacity K > 0 too below the Capacity option dropdown.", "Coordinate Values of Capacity - K with the Queue Model" );

                        }
                    capacityIncMsg1 = true;
                    return;

                    }

                    if ( item1.Length == 9 )
                    {
                    item3 = item1.Substring( 0, 9 );// UNLIMITED-

                    if ( item3 == "UNLIMITED" )
                        {
                  
                        queueSiteMaxCapacity.Value = 0;

                        }
                    }

                   if ( !capacityIncMsg2 && item1.Length == 9 )
                      {
                    item3 = item1.Substring( 0, 9 );// UNLIMITED-

                    if ( item3 == "UNLIMITED" )
                        {
                        //MessageBox.Show( "IF the site CAPACITY (K) coment is changed UNLIMITED\n" +
                        //               "- then 1st choose a model type of M/M/1 or M/M/S - UnLimited capacity,and\n" +
                        //               "  then also specify the Cacacity K = 0 too below the Capacity option dropdown.", "Coordinate Values of Capacity - K with the Queue Model" );

                        queueSiteMaxCapacity.Value = 0;

                        capacityIncMsg2 = true;
                        return;
                        }
                    }
                
                }

          

            private void quResultsInit( object sender, EventArgs e )
                {
                //--- called when tab 3 - queue main results is ENTERED 
                // PASS MAIN QUEUE PARMS

                queueSteadyStateValue1_t3.Text = queueSteadyStateValue1.Text;
               
                queueNumberServers_t3.Value = queueNumberServers.Value;
                queueSiteMaxCapacity_t3.Value = queueSiteMaxCapacity.Value;
                queueMeanValueBirth_t3.Value = queueMeanValueBirth.Value;
                queueMeanValueDeath_t3.Value = queueMeanValueDeath.Value;
                }

          
          
           

            //--- emd of class
       }


        public class MMS
            {
            static double ss1;
            static double ssPhi; // steady state
            static double s; // num of servers
            static double L, Lq;
            static double W, Wq;
            //--- mean rate of service completion for n-customers in the system is muArr
            static double [ ] muArr = new double [ 51 ];
            static double [ ] p = new double [ 100 ]; //-- probability array - per n - customers 
            static double lambda;
            /* 
             *  // received 
             *  s=num servers, 
             *  lambda=customers arrival mean rate per unit time, 
             *  mu=queues (tellers) can handle mean rate per unit timer
             */
            static public void queueCalc( int s, int lambda, int mu )
                {

                string muArrMsg=""; 

                if ( mu != 0 )
                    {
                    ss1 = 0.5 * lambda / mu;
                    MessageBox.Show( "Next we check the queues (waiting lines) will not grow unlimited,\n" +
                                    "For this we use a formula and the result must be below One for the\n" +
                                    "queues to achieve a STEADY STATE. A value of 1 or more is unsteady.","STEADY STATE CHECK" );

                    MessageBox.Show( "Passed STEADY STATE check formula: Arrivals/2*Transactions is: \n\n "+ss1.ToString( ),"** STEADY STATE CHECK PASSED **" );
                    }

                else
                    {
                    MessageBox.Show( "Passed invalid 'customers arrival mean rate per unit time' as zero" );
                    return;
                    };


                for (int ix=1; ix <= 50; ix++) 
                {
                    if ( ix <= s )
                    { muArr [ ix ] = ix * mu; }
                    else
                    { muArr [ ix ] = s * mu; }

                    muArrMsg = muArrMsg + muArr [ ix ].ToString() + ",";
                       

                }
                MessageBox.Show( "The Mean rate of service completions (per hour) for 1-50 customers is\n" + muArrMsg, "SERVICE COMPLETIONS MEAN RATE" );



                }



            }

        }
 
