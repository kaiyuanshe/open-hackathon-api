import sys

sys.path.append("..")
from hackathon.database.models import Hackathon, User, UserEmail, Register
from hackathon.database import db_adapter


class HackathonManager():
    def __init__(self, db):
        self.db = db

    def get_hackathon_by_name(self, name):
        return self.db.find_first_object(Hackathon, name=name)

    def get_hackathon_by_id(self, hackathon_id):
        return self.db.find_first_object(Hackathon, id=hackathon_id)


    def get_hackathon_stat(self, hackathon_id):
        reg_email_list = map(lambda r: r.email,
                             self.db.find_all_objects(Register, hackathon_id=hackathon_id, enabled=1))
        reg_count = len(reg_email_list)
        stat = {
            "total": reg_count,
            "hid": hackathon_id,
            "online": 0,
            "offline": reg_count
        }
        if (reg_count > 0):
            user_id_list = map(lambda ue: ue.user_id, UserEmail.query.filter(UserEmail.email.in_(reg_email_list)).all())
            online_count = User.query.filter(User.id.in_(user_id_list)).count()
            stat["online"] = online_count
            stat["offline"] = reg_count - online_count

        return stat


hack_manager = HackathonManager(db_adapter)